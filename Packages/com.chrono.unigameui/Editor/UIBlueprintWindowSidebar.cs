using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniGameUI.Editor
{
    // 藍圖視窗的右欄。分成幾個有標題的區塊，依目前選取的是什麼決定顯示哪幾個：
    //   沒有選取：工作區。
    //   選取畫面：畫面、畫面設定、從這裡出發的路由、編輯路由（選了路由或按了新增才有）、通到這裡的路由。
    //   選取「任何畫面」：任何畫面、訊號路由、編輯路由。
    public sealed partial class UIBlueprintWindow
    {
        internal const float DefaultSidebarWidth = 360;
        internal const float MinSidebarWidth = 300;
        internal const float MinCanvasWidth = 240;

        // 路由編輯區還沒儲存的內容。儲存失敗時用它把使用者填的東西放回去。
        private sealed class RouteDraft
        {
            public string Action;
            public UIRouteKind Kind;
            public string Target;
            public UIHistoryMode History;
            public string Result;
        }

        // 按了「新增路由」，還沒儲存。
        [SerializeField] private bool addingRoute;
        private RouteDraft draft;
        // 上一次儲存沒有成功的原因，顯示在出錯的欄位旁邊。
        private string actionError;
        private string targetError;
        private string routeError;
        private PopupField<UIRouteKind> kindField;
        private PopupField<string> targetField;
        private PopupField<UIHistoryMode> historyField;
        private TextField resultField;
        private VisualElement routeFields;

        /// <summary>右欄的寬度。使用者拖過之後在這個 Editor 工作階段內記住。</summary>
        internal static float SidebarWidth => Mathf.Max(MinSidebarWidth, UIBlueprintSession.SidebarWidth(DefaultSidebarWidth));

        /// <summary>記住右欄的寬度。不會小於最小寬度。</summary>
        internal static void RememberSidebarWidth(float width)
        {
            if (float.IsNaN(width) || width <= 0f)
                return;
            UIBlueprintSession.SetSidebarWidth(Mathf.Max(MinSidebarWidth, width));
        }

        /// <summary>收合或展開右欄的一個區塊，並在這個 Editor 工作階段內記住。</summary>
        internal void SetSectionCollapsed(string key, bool collapsed)
        {
            UIBlueprintSession.SetSectionCollapsed(key, collapsed);
            sidebar?.Q<Foldout>("section-" + key)?.SetValueWithoutNotify(!collapsed);
        }

        private void DrawSidebar()
        {
            sidebar.Clear();
            actionField = null;
            kindField = null;
            targetField = null;
            historyField = null;
            resultField = null;
            routeFields = null;

            // 執行中的藍圖：最上面是執行中的狀態與最近的導航，其餘的區塊照常顯示但都是唯讀。
            if (liveMode)
            {
                DrawLiveSection();
                DrawNavigationLogSection();
            }

            var isAnySource = selectedId == UIRoot.AnySource;
            var selected = isAnySource ? null : UIAuthoring.FindEntry(workspace, selectedId);
            if (!isAnySource && selected == null)
            {
                addingRoute = false;
                DrawWorkspaceSection();
                return;
            }

            var sourceId = isAnySource ? UIRoot.AnySource : selected.Id;
            if (isAnySource)
            {
                DrawAnySourceSection();
            }
            else
            {
                DrawViewSection(selected);
                DrawViewSettingsSection(selected);
            }

            var outgoing = workspace.Routes.Where(route => route.Source == sourceId).ToArray();
            var current = outgoing.FirstOrDefault(route => route.Action == selectedAction);
            if (current != null || liveMode)
                addingRoute = false;
            DrawOutgoingSection(sourceId, outgoing, current, isAnySource);
            if (current != null || addingRoute)
                DrawRouteEditorSection(sourceId, current, isAnySource);
            if (!isAnySource)
                DrawIncomingSection(selected);
            draft = null;
        }

        // 右欄的一個區塊：有標題列與邊界，可以收合。收合的狀態在這個 Editor 工作階段內記住。
        private Foldout AddSection(string key, string title)
        {
            var section = new Foldout
            {
                name = "section-" + key,
                text = title,
                value = !UIBlueprintSession.IsSectionCollapsed(key),
            };
            section.AddToClassList("sidebar-section");
            section.RegisterValueChangedCallback(evt =>
            {
                // 區塊裡的勾選框也會送出同一種事件，只處理區塊自己的。
                if (evt.target == section)
                    SetSectionCollapsed(key, !evt.newValue);
            });
            sidebar.Add(section);
            return section;
        }

        private static Label AddInfo(VisualElement parent, string label, string value, string name = null)
        {
            var row = new VisualElement();
            row.AddToClassList("info-row");
            var caption = new Label(label);
            caption.AddToClassList("info-label");
            row.Add(caption);
            var content = new Label(value) { name = name };
            content.AddToClassList("info-value");
            row.Add(content);
            parent.Add(row);
            return content;
        }

        private static Label AddHelp(VisualElement parent, string text, string name = null)
        {
            var help = new Label(text) { name = name };
            help.AddToClassList("field-help");
            parent.Add(help);
            return help;
        }

        private static Label AddError(VisualElement parent, string text, string name)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            var error = new Label(text) { name = name };
            error.AddToClassList("field-error");
            parent.Add(error);
            return error;
        }

        // 沒有選取任何節點：工作區的摘要。
        private void DrawWorkspaceSection()
        {
            var section = AddSection("workspace", "工作區");
            var scene = workspace.gameObject.scene;
            AddInfo(section, "Scene", string.IsNullOrEmpty(scene.name) ? "尚未儲存的 Scene" : scene.name, "workspace-scene");
            AddInfo(section, "模式", workspace.Mode == UIAuthoringMode.Prefab ? "Prefab（頁面也是 Prefab）" : "單一 Scene",
                "workspace-mode");

            var pages = workspace.Entries.Where(entry => entry.Kind == UIEntryKind.Page).Select(entry => entry.Id).ToList();
            var startChoices = new List<string> { "" };
            startChoices.AddRange(pages);
            var start = new PopupField<string>("起始頁", startChoices,
                Mathf.Max(0, startChoices.IndexOf(workspace.StartPageId)), DisplayView, DisplayView)
            {
                name = "start-page",
                tooltip = "載入 Scene 之後第一個顯示的頁面。",
            };
            start.RegisterValueChangedCallback(evt => TryEdit(() => UIAuthoring.SetStartPage(workspace, evt.newValue)));
            start.SetEnabled(!liveMode);
            section.Add(start);

            AddInfo(section, "頁面", $"{pages.Count} 個", "workspace-pages");
            AddInfo(section, "Popup", $"{workspace.Entries.Count(entry => entry.Kind == UIEntryKind.Popup)} 個",
                "workspace-popups");
            AddInfo(section, "訊號路由", $"{workspace.Routes.Count(route => route.IsSignal)} 條", "workspace-signals");
            AddHelp(section, liveMode
                ? "選取節點可以查看畫面與它的路由，用「觸發」按鈕走一條路由。Play Mode 中不能修改藍圖。"
                : "選取節點可以檢視畫面、編輯它的路由。從節點右邊的連接點拖線到另一個節點，可以建立路由。" +
                  "雙擊節點會定位到 Scene 裡的畫面。", "workspace-hint");
        }

        // 選取畫面：名稱、ID、種類、起始頁、定位，Popup 另有疊在頁面上預覽。
        private void DrawViewSection(UIEntry selected)
        {
            var section = AddSection("view", "畫面");
            var heading = new VisualElement();
            heading.AddToClassList("section-heading");
            var title = new Label(selected.Title) { name = "view-title" };
            title.AddToClassList("sidebar-title");
            heading.Add(title);
            AddBadge(heading, EntryKindLabel(selected.Kind),
                selected.Kind == UIEntryKind.Popup ? "badge-popup" : "badge-page").name = "view-kind";
            section.Add(heading);
            AddInfo(section, "ID", selected.Id, "view-id");

            if (selected.Kind == UIEntryKind.Page)
            {
                if (selected.Id == workspace.StartPageId)
                {
                    AddInfo(section, "起始頁", "是。載入 Scene 之後第一個顯示的就是這個頁面。", "view-is-start");
                }
                else if (!liveMode)
                {
                    section.Add(new Button(() => TryEdit(() => UIAuthoring.SetStartPage(workspace, selected.Id)))
                    {
                        name = "set-start-page",
                        text = "設為起始頁",
                        tooltip = "載入 Scene 之後第一個顯示這個頁面。",
                    });
                }
            }

            var focus = new Button(() => RequestFocus(selected.Id))
            {
                name = "focus-view",
                text = liveMode ? "在 Hierarchy 選取" : "在 Scene 中定位",
                tooltip = liveMode
                    ? "在 Hierarchy 選取執行中的這個畫面。畫面還沒建立（例如沒開過的 Popup）時不能按。在藍圖上雙擊節點也可以。"
                    : "把 Scene 視窗移到這個畫面。在藍圖上雙擊節點也可以。",
            };
            focus.SetEnabled(!liveMode || HasRunningView(selected.Id));
            section.Add(focus);
            if (liveMode)
                return;

            var pages = workspace.Entries.Where(entry => entry.Kind == UIEntryKind.Page).Select(entry => entry.Id).ToList();
            if (selected.Kind == UIEntryKind.Popup && pages.Count > 0)
            {
                var context = new PopupField<string>("疊在頁面", pages, 0, DisplayView, DisplayView)
                {
                    name = "preview-page",
                    tooltip = "預覽這個 Popup 疊在哪一個頁面上的樣子。",
                };
                section.Add(context);
                section.Add(new Button(() => UIAuthoring.PreviewPopupOnPage(workspace, selected.Id, context.value))
                {
                    name = "preview-popup",
                    text = "預覽疊加",
                });
            }

            if (UIAuthoring.HasReviewOverrides(selected))
            {
                section.Add(new HelpBox("這個預覽有待套用的 Prefab 覆寫。執行期使用的是已儲存的 Prefab。",
                    HelpBoxMessageType.Warning));
                section.Add(new Button(() => TryEdit(() => UIAuthoring.ApplyReviewOverrides(workspace, selected.Id)))
                {
                    name = "apply-overrides",
                    text = "套用到 Prefab",
                });
            }
        }

        // 畫面設定：每個選項都有一句說明。
        private void DrawViewSettingsSection(UIEntry selected)
        {
            var section = AddSection("view-settings", "畫面設定");
            // 確認框用 ConfirmAsync 開啟、自己關閉，自動視為由程式開啟，不能取消。
            var isConfirm = UIAuthoring.IsConfirmPopup(selected.Review) || UIAuthoring.IsConfirmPopup(selected.Prefab);
            var help = isConfirm
                ? "確認框由 ConfirmAsync 開啟並自己關閉，自動視為由程式開啟，不能取消。"
                : selected.Kind == UIEntryKind.Page
                    ? "進來的方式不靠路由時勾選，例如需要程式給資料才能顯示的畫面。勾選後 Validate 不檢查它走不走得到，" +
                      "但頁面照樣要有出口：這一頁發生的換頁請畫成這一頁的路由。去處由遊戲狀態決定，不等於不靠路由。"
                    : "進來的方式不靠路由時勾選，例如程式開啟、自己關閉的 Popup。" +
                      "勾選後 Validate 不檢查它走不走得到、有沒有出口。";
            var byCode = new Toggle("由程式開啟")
            {
                name = "navigated-by-code",
                value = UIAuthoring.IsNavigatedByCode(selected),
                tooltip = help,
            };
            byCode.SetEnabled(!isConfirm && !liveMode);
            byCode.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == byCode)
                    SetNavigatedByCode(selected.Id, evt.newValue);
            });
            section.Add(byCode);
            AddHelp(section, help, "navigated-by-code-help");
        }

        // 選取「任何畫面」節點：說明它的用途。
        private void DrawAnySourceSection()
        {
            var section = AddSection("any-source", AnySourceTitle);
            var heading = new VisualElement();
            heading.AddToClassList("section-heading");
            var title = new Label(AnySourceTitle) { name = "view-title" };
            title.AddToClassList("sidebar-title");
            heading.Add(title);
            AddBadge(heading, "訊號", "badge-signal").name = "view-kind";
            section.Add(heading);
            AddHelp(section, "從這個節點拉出去的是訊號路由：不論目前顯示哪個畫面都成立。" +
                "遊戲程式用 root.SignalAsync(動作, 資料) 送出；有導航正在進行時，訊號會等它結束再執行。" +
                "它不是畫面，沒有線可以連進來。", "any-source-help");
        }

        // 從這裡出發的路由：整列可以點，選中的那一列有明顯的標示。
        private void DrawOutgoingSection(string sourceId, UIRoute[] outgoing, UIRoute current, bool isAnySource)
        {
            var section = AddSection("routes", isAnySource ? "訊號路由" : "從這裡出發的路由");
            if (outgoing.Length == 0)
            {
                AddHelp(section, liveMode
                    ? (isAnySource ? "沒有訊號路由。" : "這個畫面沒有路由。")
                    : isAnySource
                        ? "還沒有訊號路由。按「新增路由」，或從節點右邊的連接點拖線到目標畫面。"
                        : "這個畫面還沒有路由。按「新增路由」，或從節點右邊的連接點拖線到另一個節點。", "routes-empty");
            }
            foreach (var route in outgoing)
            {
                // 執行中的藍圖：每一列右邊多一個「觸發」按鈕。
                VisualElement parent = section;
                if (liveMode)
                {
                    parent = new VisualElement();
                    parent.AddToClassList("route-row-live");
                    section.Add(parent);
                }
                var row = AddRouteRow(parent, "route-row", route, route.Action, RouteTargetText(route),
                    () => SelectRoute(sourceId, route.Action));
                row.EnableInClassList("route-row-selected", route == current);
                if (liveMode)
                    AddTriggerButton(parent, route);
            }
            if (liveMode)
            {
                if (outgoing.Length > 0)
                {
                    AddHelp(section, UIWalk.IsRunning
                        ? "走訪進行中，暫時不能觸發。"
                        : isAnySource
                            ? "「觸發」用 SignalAsync 送出訊號，隨時可以按。不帶資料，需要資料的畫面會顯示失敗。"
                            : "「觸發」用 TriggerAsync 走這條路由，這個畫面是最上層時才能按。不帶資料，需要資料的畫面會顯示失敗。",
                        "trigger-help");
                }
                return;
            }
            section.Add(new Button(BeginAddRoute)
            {
                name = "add-route",
                text = "新增路由",
                tooltip = "在下面的「編輯路由」填寫新的路由。",
            });
        }

        // 通到這裡的路由：唯讀，列出哪些畫面的哪個動作會到這個畫面，包含訊號路由。點一列會選取來源。
        private void DrawIncomingSection(UIEntry selected)
        {
            var section = AddSection("incoming", "通到這裡的路由");
            var incoming = workspace.Routes.Where(route => UIRoute.TargetsEntry(route.Kind) && route.Target == selected.Id)
                .ToArray();
            if (incoming.Length == 0)
            {
                AddHelp(section, selected.Id == workspace.StartPageId
                    ? "沒有路由通到這個畫面。它是起始頁，載入 Scene 之後會自動顯示。"
                    : "沒有路由通到這個畫面。它要由程式顯示，或者還沒有接上。", "incoming-empty");
                return;
            }
            foreach (var route in incoming)
            {
                AddRouteRow(section, "incoming-row", route, DisplayView(route.Source), route.Action,
                    () => SelectRoute(route.Source, route.Action))
                    .tooltip = "點一下選取來源節點與這條路由。";
            }
        }

        // 路由的一列：主要的文字、種類的徽章、次要的文字。整列是一個按鈕。
        private static Button AddRouteRow(VisualElement parent, string name, UIRoute route, string primary,
            string secondary, Action clicked)
        {
            var row = new Button(clicked) { name = name, userData = route };
            row.AddToClassList("route-row");
            var main = new Label(primary);
            main.AddToClassList("route-row-primary");
            row.Add(main);
            AddBadge(row, KindLabel(route.Kind), "kind-" + route.Kind);
            if (!string.IsNullOrEmpty(secondary))
            {
                var detail = new Label(secondary);
                detail.AddToClassList("route-row-secondary");
                row.Add(detail);
            }
            parent.Add(row);
            return row;
        }

        private string RouteTargetText(UIRoute route)
        {
            return UIRoute.TargetsEntry(route.Kind) ? "→ " + DisplayView(route.Target) : "";
        }

        /// <summary>「新增路由」：打開空白的編輯區。</summary>
        internal void BeginAddRoute()
        {
            addingRoute = true;
            selectedAction = null;
            ClearRouteErrors();
            DrawSidebar();
        }

        private void ClearRouteErrors()
        {
            actionError = null;
            targetError = null;
            routeError = null;
        }

        // 編輯路由：選了路由或按了新增才出現。只顯示這個行為用得到的欄位。
        private void DrawRouteEditorSection(string sourceId, UIRoute current, bool isAnySource)
        {
            var section = AddSection("route-editor", liveMode ? "路由內容" : current == null ? "新增路由" : "編輯路由");
            var values = draft ?? new RouteDraft
            {
                Action = current?.Action ?? "",
                Kind = current?.Kind ?? UIRouteKind.ShowPage,
                Target = current?.Target,
                History = current?.History ?? UIHistoryMode.Push,
                Result = current?.Result ?? "",
            };

            actionField = new TextField("動作")
            {
                name = "route-action",
                value = values.Action,
                tooltip = "按鈕或程式用這個名稱觸發路由。只能用英文字母、數字、底線或連字號。",
            };
            section.Add(actionField);
            AddError(section, actionError, "route-action-error");

            // 訊號路由只能切頁、開啟 Popup 或送出事件。
            var kinds = ((UIRouteKind[])Enum.GetValues(typeof(UIRouteKind)))
                .Where(kind => !isAnySource || UIAuthoring.IsSignalKind(kind)).ToList();
            kindField = new PopupField<UIRouteKind>("行為", kinds,
                kinds.Contains(values.Kind) ? values.Kind : UIRouteKind.ShowPage, KindChoice, KindChoice)
            {
                name = "route-kind",
            };
            section.Add(kindField);

            routeFields = new VisualElement { name = "route-fields" };
            section.Add(routeFields);
            BuildRouteFields(routeFields, kindField.value, values.Target, values.History, values.Result);
            kindField.RegisterValueChangedCallback(evt => RouteKindChanged(evt.newValue));
            AddError(section, routeError, "route-error");

            // 線的中繼點：只是整理線的走向。
            if (current != null && (UIRoute.TargetsEntry(current.Kind) || current.Waypoints.Count > 0))
            {
                var count = current.Waypoints.Count;
                AddInfo(section, "中繼點", count == 0
                    ? liveMode ? "沒有。" : "沒有。在線上雙擊可以新增。"
                    : $"{count} 個", "route-waypoints");
                if (!liveMode)
                {
                    var clear = new Button(() => ClearWaypoints(current))
                    {
                        name = "clear-waypoints",
                        text = "清除中繼點",
                        tooltip = "拿掉這條線所有的中繼點，線回到直接連到目的地。可以 Undo。",
                    };
                    clear.SetEnabled(count > 0);
                    section.Add(clear);
                }
            }
            if (liveMode)
            {
                // 唯讀：看得到內容，不能修改，也沒有儲存與刪除。
                actionField.SetEnabled(false);
                kindField.SetEnabled(false);
                routeFields.SetEnabled(false);
                AddHelp(section, "Play Mode 中不能修改路由。", "route-read-only");
                return;
            }

            var buttons = new VisualElement();
            buttons.AddToClassList("route-buttons");
            section.Add(buttons);
            buttons.Add(new Button(() => SubmitRoute(sourceId, current))
            {
                name = "save-route",
                text = current == null ? "新增" : "儲存",
            });
            if (current == null)
            {
                buttons.Add(new Button(CancelAddRoute) { name = "cancel-route", text = "取消" });
                return;
            }

            // 刪除和其他按鈕分開放，顏色也不同，避免按錯。
            var spacer = new VisualElement();
            spacer.AddToClassList("route-buttons-spacer");
            buttons.Add(spacer);
            var delete = new Button(() => DeleteRoute(sourceId, current.Action))
            {
                name = "delete-route",
                text = "刪除這條路由",
            };
            delete.AddToClassList("danger-button");
            buttons.Add(delete);
        }

        // 依行為決定要哪些欄位：切頁與開啟 Popup 有目的地與返回歷史，關閉 Popup 有關閉結果，其餘沒有。
        private void BuildRouteFields(VisualElement fields, UIRouteKind kind, string target, UIHistoryMode history,
            string result)
        {
            fields.Clear();
            targetField = null;
            historyField = null;
            resultField = null;

            if (UIRoute.TargetsEntry(kind))
            {
                var isPopup = kind == UIRouteKind.OpenPopup;
                var type = isPopup ? UIEntryKind.Popup : UIEntryKind.Page;
                var choices = new List<string> { "" };
                choices.AddRange(workspace.Entries.Where(entry => entry.Kind == type).Select(entry => entry.Id));
                targetField = new PopupField<string>("目的地", choices, Mathf.Max(0, choices.IndexOf(target)),
                    DisplayView, DisplayView)
                {
                    name = "route-target",
                    tooltip = isPopup ? "要開啟的 Popup。" : "要切到的頁面。",
                };
                fields.Add(targetField);
                AddError(fields, targetError, "route-target-error");

                // 「返回歷史」只有切頁與開啟 Popup 有。Popup 沒有 Replace：只分可以返回與不可返回。
                var modes = isPopup
                    ? new List<UIHistoryMode> { UIHistoryMode.Push, UIHistoryMode.Clear }
                    : new List<UIHistoryMode> { UIHistoryMode.Push, UIHistoryMode.Replace, UIHistoryMode.Clear };
                Func<UIHistoryMode, string> label = isPopup ? PopupHistoryLabel : HistoryLabel;
                historyField = new PopupField<UIHistoryMode>("返回歷史", modes,
                    modes.Contains(history) ? history : UIHistoryMode.Push, label, label)
                {
                    name = "route-history",
                    tooltip = isPopup
                        ? "不可返回的 Popup：開啟時清空歷史，返回鍵與 Back 路由無效，只能用 ClosePopup 路由、" +
                          "View.Close() 或點遮罩關閉。"
                        : "切到目的地之後，離開的頁面要不要留在返回歷史裡。",
                };
                historyField.style.display = DisplayStyle.Flex;
                fields.Add(historyField);
            }

            if (kind == UIRouteKind.ClosePopup)
            {
                resultField = new TextField("關閉結果")
                {
                    name = "route-result",
                    value = result ?? "",
                    tooltip = "關閉時回傳給 OpenPopupForResultAsync 的字串。留空時不回傳結果。",
                };
                resultField.style.display = DisplayStyle.Flex;
                fields.Add(resultField);
            }

            if (kind == UIRouteKind.Back)
                AddHelp(fields, "關閉最上層的 Popup；沒有 Popup 時回到上一頁。", "route-kind-help");
            else if (kind == UIRouteKind.Emit)
                AddHelp(fields, "不切換畫面，只通知頁面腳本的 OnAction 與工作區的 ActionEmitted。", "route-kind-help");
        }

        /// <summary>「行為」換了：欄位換成新的行為用得到的那幾個。</summary>
        internal void RouteKindChanged(UIRouteKind kind)
        {
            if (routeFields == null)
                return;
            targetError = null;
            BuildRouteFields(routeFields, kind, null, UIHistoryMode.Push, "");
        }

        // 儲存編輯區的內容。有問題時不儲存，把原因顯示在出錯的欄位旁邊，填過的內容留著。
        private void SubmitRoute(string sourceId, UIRoute current)
        {
            ClearRouteErrors();
            var values = new RouteDraft
            {
                Action = actionField.value ?? "",
                Kind = kindField.value,
                Target = targetField?.value,
                History = historyField?.value ?? UIHistoryMode.Push,
                Result = resultField?.value ?? "",
            };

            try
            {
                UIAuthoring.CheckId(values.Action);
            }
            catch (ArgumentException)
            {
                actionError = "動作名稱不能是空的，而且只能用英文字母、數字、底線或連字號。";
            }
            if (actionError == null && workspace.Routes.Any(route =>
                    route != current && route.Source == sourceId && route.Action == values.Action))
            {
                actionError = $"這裡已經有動作「{values.Action}」的路由。要修改它，請從上面的清單選取。";
            }
            if (UIRoute.TargetsEntry(values.Kind) && string.IsNullOrEmpty(values.Target))
                targetError = values.Kind == UIRouteKind.OpenPopup ? "請選擇要開啟的 Popup。" : "請選擇要切到的頁面。";

            if (actionError == null && targetError == null)
            {
                try
                {
                    SaveRoute(sourceId, current?.Action, values.Action, values.Kind, values.Target, values.History,
                        values.Result);
                    addingRoute = false;
                    Rebuild();
                    return;
                }
                catch (Exception exception)
                {
                    routeError = exception.Message;
                }
            }

            draft = values;
            DrawSidebar();
        }

        private void CancelAddRoute()
        {
            addingRoute = false;
            ClearRouteErrors();
            DrawSidebar();
        }

        private void DeleteRoute(string sourceId, string action)
        {
            ClearRouteErrors();
            TryEdit(() =>
            {
                UIAuthoring.Disconnect(workspace, sourceId, action);
                selectedAction = null;
            });
        }

        private void RequestFocus(string id)
        {
            if (liveMode)
            {
                SelectRunningView(id);
                return;
            }
            if (FocusRequested != null)
                FocusRequested(id);
            else
                UIAuthoring.FocusReview(workspace, id);
        }

        // 「行為」選單顯示的文字：和節點、清單裡的徽章用同一個說法，後面附上計畫與程式用的名稱。
        private static string KindChoice(UIRouteKind kind)
        {
            return $"{KindLabel(kind)}（{kind}）";
        }
    }
}
