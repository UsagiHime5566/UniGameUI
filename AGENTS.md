# 給 agent 的工作規則

這個 repo 是 UniGameUI 套件的開發與測試專案。套件本體在 `Packages/com.chrono.unigameui`，其餘是用來開發和驗證的 Unity 專案。

## 每一輪開始前

1. 讀 `ROADMAP.md`：已定案的決定、四批的範圍、目前這一批的詳細規格。
2. 讀 `CODEX_REVIEW.md`：審核發現的問題與各項目的原始說明。
3. 讀 `Packages/com.chrono.unigameui/Documentation~/AgentGuide.md`：套件目前的用法。

## 範圍

- 只做使用者指定的那一批。
- 只有寫了詳細規格的批次可以實作。只列出範圍的批次不要先做。
- 規格沒寫到、而且會影響公開 API 或使用方式的事，先問使用者，不要自己決定。
- `ROADMAP.md` 的「已定案的決定」不要推翻。覺得有問題時提出來討論。

## 實作

- 公開 API 可以直接修改，目前沒有專案在使用，不必保留相容層。
- `#if DOTWEEN` 要保留：套件必須在沒有 DOTween 時也能編譯。
- 每一項都要補測試，既有測試要全數通過。
- 公開 API 或使用方式有變動時，同步更新 `README.md`、`AgentGuide.md`、`CHANGELOG.md`。
- 文件與 Editor 介面文字使用繁體中文。
- 程式碼要正確排版：縮排一致、適當換行。

## 驗證

- 用 Unity 6000.3.19f1 跑 EditMode 與 PlayMode 測試，測試環境要安裝 DOTween。
- 使用者的 Editor 開著專案時，命令列拿不到專案鎖，請在專案的副本上驗證。做法見 `CODEX_REVIEW.md` 的「驗證方法」。
- 只能在互動 Editor 裡操作的部分（對話框、滑鼠操作）無法自動測試，請在交付說明裡列出來，讓使用者手動確認。

## 版控

- 不要 commit DOTween 的任何檔案。
- **不要 commit `ProjectSettings/ProjectSettings.asset` 裡的 `DOTWEEN` define。** DOTween 不在這個 repo 裡，define 一旦進版控，別台電腦開專案時套件會編譯失敗。
- 與這一輪工作無關的 `ProjectSettings` 變更不要 commit。
- 不要 push，由使用者自己推送。
- 交付時在 `package.json` 升版號，但不要打版本標籤。標籤等審核通過後才打。

## 交付

- 更新 `ROADMAP.md` 的批次狀態，以及 `CODEX_REVIEW.md` 的狀態表。
- 交付說明要寫清楚：做了哪些項目、測試結果（通過、失敗、略過各幾個）、哪些沒有驗證、哪些需要使用者手動確認。
- 沒做完或沒驗證的項目要直接說明，不要當成已完成。
