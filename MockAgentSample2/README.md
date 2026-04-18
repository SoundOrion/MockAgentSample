# MockAgentSample

## 概要

このサンプルは、**AIエージェント風の処理フロー** を C# の Console アプリで体験するための最小構成です。
実際のAI APIは使わず、**ダミーAI（MockAiPlanner）** が次の行動を決定します。

ユーザー依頼:

```text
売上CSVを読んで、異常値があれば要約してSlackに送って
```

処理フロー:

1. 売上CSVを読む
2. 売上を分析する
3. 異常値があれば Slack 通知する（このサンプルではコンソール出力）
4. 最終結果を返す

---

## 目的

このサンプルで学べること:

* エージェント的な「複数ステップ実行」
* 状態（Context）を持ちながら進む処理
* ツール実行（CSV読込・分析・通知）
* AI部分と業務ロジックの分離
* 将来、本物のLLMへ差し替えやすい設計

---

## プロジェクト作成

```bash
dotnet new console -n MockAgentSample
cd MockAgentSample
```

その後、`Program.cs` をサンプルコードに置き換えてください。

---

## ファイル構成

```text
MockAgentSample/
 ├─ Program.cs
 └─ sales.csv   （初回起動時に自動生成）
```

---

## 実行方法

```bash
dotnet run
```

---

## 実行結果イメージ

```text
[Agent] Step 1
ReadSalesCsv

[Agent] Step 2
AnalyzeSales

[Agent] Step 3
PostToSlack

===== FINAL RESULT =====
売上を分析しました。平均売上は 1596.43 で、異常値は 1 件見つかりました。 Slackに通知しました。
```

---

## アーキテクチャ

### 1. MockAiPlanner

ダミーAIです。現在の状態を見て、次のアクションを決定します。

例:

* CSV未読 → `ReadSalesCsv`
* 未分析 → `AnalyzeSales`
* 異常値あり → `PostToSlack`
* 完了 → Finish

### 2. SalesTools

実務ロジック担当です。

* CSV読み込み
* 売上平均算出
* 異常値判定

### 3. SlackTools

通知担当です。
このサンプルでは Slack API の代わりにコンソール出力します。

### 4. MockAgentOrchestrator

全体進行管理です。

* Planner に判断させる
* Tool を実行する
* Context を更新する
* 完了までループする

---

## Context（状態管理）

保持している状態例:

* CSV読み込み済みか
* 分析済みか
* Slack送信済みか
* 売上データ
* 分析結果

これにより、1回のAPI応答ではなく、**複数ステップで仕事を進めるエージェント風挙動** を実現しています。

---

## 異常値判定ロジック

簡易版として以下条件です。

```text
売上 > 平均の1.8倍
または
売上 < 平均の0.5倍
```

本番では以下に変更可能です。

* 前日比
* 移動平均
* Z-score
* 店舗別比較
* 季節性考慮

---

## 本物AIへ差し替える場所

主に以下です。

```csharp
MockAiPlanner.DecideNextStep()
```

現在は固定ルールですが、将来的には:

* OpenAI API
* Azure OpenAI
* Semantic Kernel
* Agent Framework

などへ置き換え可能です。

---

## 設計上のおすすめ

AIには **判断だけ** させるのがおすすめです。

* どのツールを呼ぶか → AI
* CSV解析ロジック → C#
* Slack送信 → C#
* DB保存 → C#

この分離により、安定しやすく保守しやすい構成になります。

---

## 次の拡張案

### ツール追加

* SaveReport()
* SendEmail()
* CreateDashboard()

### Web API 化

ASP.NET Core で以下のようにできます。

```text
POST /run-agent
```

### 本物Slack連携

Webhook URL を使って実送信。

### 本物AI連携

Planner 部分を LLM に置き換える。

---

## このサンプルの価値

単なる「APIを1回叩く」構成から、次の段階へ進めます。

* 状況判断
* ツール選択
* 複数ステップ処理
* 条件分岐
* 自動通知

つまり **エージェント入門として最適なサンプル** です。