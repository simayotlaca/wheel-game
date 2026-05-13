<div align="center">

### 🎀 Vertigo Wheel of Fortune 🎡

<sub><i>Open cases, collect rewards, and test your luck.</i></sub>

<br/>

<img alt="Unity 2021 LTS" src="https://img.shields.io/badge/Unity_2021_LTS-D4799F?style=flat-square&logo=unity&logoColor=white"/>&nbsp;<img alt="Android" src="https://img.shields.io/badge/Android-1F1F23?style=flat-square&logo=android&logoColor=D4799F"/>&nbsp;<img alt="Pure C#" src="https://img.shields.io/badge/Pure_C%23-D4799F?style=flat-square&logo=csharp&logoColor=white"/>&nbsp;<img alt="Multi-Aspect UI" src="https://img.shields.io/badge/Multi--Aspect_UI-1F1F23?style=flat-square&logoColor=D4799F"/>

<br/><br/>

<img src="Docs/Screenshots/gameplay_20-9.gif" width="720" alt="Gameplay preview"/>

</div>

<br/>

A Unity case study for the **Vertigo Games** developer brief. Spin the wheel, hoard rewards in a pending stash, exit before a bomb ends the run.

#### 🌸 How to Play

| Action | Effect |
|---|---|
| Tap **SPIN** | The wheel turns; a reward lands in your *pending* stash |
| Tap **EXIT** | Pending rewards move into your inventory |
| Every 5th spin | Safe zone, no bombs |
| Every 30th spin | Super zone |
| Normal zones | A bomb may land. If it does, the run ends |
| Revive | Pay gold to keep your loot. Each revive costs more |
| Persistence | Banked rewards survive between sessions (PlayerPrefs) |

#### 🌼 How to Run

1. Open the project in **Unity 2021.3.45f2 LTS**
2. Run `Vertigo → Build → Full Rebuild` once after a fresh checkout
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**

#### ✨ Technical Highlights

- Pure C# wheel logic separated from the Unity scene lifecycle
- Deterministic rebuild pipeline for scene/UI generation
- ScriptableObject-driven reward and zone configuration
- Lightweight pooled UI elements for repeated spin flows

**Animation Stack** &nbsp;·&nbsp; *PrimeTween, struct-based, allocation-conscious*

I used PrimeTween for UI/gameplay transitions because its allocation-conscious API and struct-based tween flow fit well with the project's lightweight runtime architecture. The wheel relies heavily on chained transitions, strike animations, overlays, and repeated UI motion during spins, so keeping animation flow predictable and easy to orchestrate was more important than building a custom tween solution from scratch.

#### ⚔️ Architecture

```text
┌──────────────────────────────────────────────────────────┐
│  WheelLogic   (pure C#, no MonoBehaviour)                │
│  Spin(zone) → SpinResult                                 │
└────────────────────────┬─────────────────────────────────┘
                         │  called by
                         ▼
┌──────────────────────────────────────────────────────────┐
│  WheelController   (event bridge: logic ↔ UI)            │
└────────────────────────┬─────────────────────────────────┘
                         │  emits
     ┌───────────────────┼───────────────────┐
     ▼                   ▼                   ▼
 OnZoneChanged     OnRewardEarned       OnDeathHit
 OnRewardsBanked   OnRevived            OnRunEnded
     │                   │                   │
     └───────────────────┼───────────────────┘
                         │  subscribed by
                         ▼
┌──────────────────────────────────────────────────────────┐
│  Presentation Layer                                      │
│    ├─ UI Panels                                          │
│    │     HUD · popups · reward list · MetaProgress       │
│    ├─ ExitFlow / DeathFlow                               │
│    │     RunExitController · revive · bank rewards       │
│    └─ MetaProgressionService                             │
│          per-run weapon points                           │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  Persistence                                             │
│    PlayerProgress  →  PlayerPrefs                        │
│      ├─ writes  · bank · revive · app pause · quit       │
│      └─ reads   · Start                                  │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  Rebuild Pipeline                                        │
│    Vertigo → Build → Full Rebuild                        │
│      ├─ WheelDistributionApplier.Apply()                 │
│      └─ WheelSceneSetup.Build()                          │
└──────────────────────────────────────────────────────────┘
```

Spin states live in `Assets/Scripts/Wheel/Controller/`: `ReadyState`, `TurningState`, `LandingState`, `RewardState`, `DeathState`, `PostReviveReadyState`. They derive from `WheelStateBase`. States never call each other directly; only `WheelController` performs transitions.

**Press SPIN**

1. Spin button calls `WheelController.RequestSpin()`
2. `WheelLogic.Spin(zone)` produces a `SpinResult` (slice, amount, bomb flag)
3. `WheelView.SpinTo(...)` runs the PrimeTween rotation
4. On stop, the reward enters `RewardInventory` as pending. If it was a bomb, the controller transitions to `DeathState`
5. Tap **EXIT** to move pending rewards into the banked inventory

**Logic and UI** &nbsp;·&nbsp; `WheelLogic` is pure C# with no scene dependency. The UI subscribes to `WheelController` events: `OnZoneChanged`, `OnRewardEarned`, `OnDeathHit`, `OnRewardsBanked`, `OnRevived`, `OnRunEnded`.

**ExitFlow** &nbsp;·&nbsp; `RunExitController` orchestrates the exit and death panels (`ExitFlowState`). In the exit flow, pending rewards move into inventory. In the death flow, the revive button calls `WheelController.TryRevive()`. Revive cost grows each time: `reviveCurrencyCost * (1 + revive_count)`.

**MetaProgress** &nbsp;·&nbsp; `MetaProgressionService` tracks per-run weapon points and reflects them on the MetaProgress panel. Resets when the run ends.

**Persistence** &nbsp;·&nbsp; `PlayerProgress` writes cash, gold and banked rewards to PlayerPrefs on bank, revive, app pause and quit. Reads once on `Start`.

**Full Rebuild** &nbsp;·&nbsp; `Vertigo → Build → Full Rebuild` runs `WheelDistributionApplier.Apply()` (zone distributions) and `WheelSceneSetup.Build()` (Canvas, wheel, UI hierarchy).

#### 🍓 Project Structure

<table align="center" border="0">
<tr><td>

> **Core Systems**
> Game rules, object pooling, and shared formatters.

> **Wheel System**
> Spin flow, state machine, reward sampling, and slice rendering.

> **UI Layer**
> HUD, popups, reward list, MetaProgress, and button systems.

> **Exit Flow**
> Run exit, death panels, revive flow.

> **Config Data**
> ScriptableObject-driven rewards, zones, and theme setup.

> **Editor Tools**
> Scene & UI builders, layout passes, validation audits.

</td></tr>
</table>

<details>
<summary>Folder tree</summary>

```
Assets/
└─ Scripts/
   ├─ Core/                — ObjectPool, GameRules ve temel sistemler
   ├─ Wheel/
   │  ├─ Controller/  ⎫
   │  ├─ Logic/       ⎬─ Çark mekaniği, state machine, sampler ve spin
   │  └─ View/        ⎭
   │  └─ Config/
   │     ├─ Wheel/    ⎫
   │     ├─ Zone/     ⎬─ ScriptableObject şablonları
   │     └─ Reward/   ⎭
   │  ├─ ExitFlow/       ⎫
   │  ├─ MetaProgress/   ⎬─ Çıkış/Ölüm akışı, silah ilerlemesi, PlayerPrefs
   │  └─ Persistence/    ⎭
   └─ Editor/
      ├─ Builders/  ⎫
      ├─ Layout/    ⎬─ Editor araçları: scene + UI builder, inspector
      └─ Drawers/   ⎭

├─ ScriptableObjects/  ⎫
├─ Prefabs/            ⎪
├─ Atlases/            ⎬─ Derlenmiş asset'ler, prefab'lar, kütüphaneler, sahne
├─ Plugins/            ⎪
└─ Scenes/             ⎭
```

</details>

#### 🎨 Tech Stack

- **PrimeTween** for UI animation (panels, scale punches, wheel rotation)
- One custom particle effect for the reward-fly burst
- Sprite Atlas split into 6 categories (Icon, Spin, Button, Panel, Frame, VFX)
- **TextMeshPro** on every label
- Canvas Scaler `ScaleWithScreenSize`, reference 1920×1080, Expand mode

#### 🪖 Performance Notes

The hot path is the spin loop. Every choice below keeps it allocation-free, draw-call-light, and free of mid-spin frame spikes.

**Allocation discipline**

- `WheelLogic` runs as pure C# with no `MonoBehaviour`, no `transform` access, no `Resources.Load`. The spin pipeline returns a `SpinResult` value type.
- ScriptableObjects under `Assets/Configs/` hold all content. Data is referenced, not parsed, at runtime. No JSON, no PlayerPrefs reads inside the spin loop.
- Reward icons and reward list rows pooled through `ObjectPool`. Zero `Instantiate` or `Destroy` during a run.
- PrimeTween for every UI tween. Struct based, no per-tween heap allocations, no coroutine churn.

**Render cost**

- Sprite Atlas split into 6 buckets so wheel, side panels and HUD batch within a single draw call group each.
- Decorative graphics ship with `raycastTarget` and `maskable` off. An editor audit enforces this so `GraphicRaycaster` only walks interactive nodes.
- Single Canvas Scaler (`ScaleWithScreenSize`, 1920×1080, Expand). One layout serves 20:9, 16:9 and 4:3 without per-aspect prefab forks. Dynamic widgets are isolated from static frames to limit Canvas rebuilds.
- TextMeshPro labels with `*_value` suffix isolate dynamic writes; static labels never call `SetText` after layout.

**Build path**

- Single scene (`SampleScene.unity`). No async loads, no additive scenes.
- `Vertigo → Build → Full Rebuild` reconstructs the UI from configs at edit time. Nothing rebuilds itself at runtime.

#### 💎 Engineering Decisions

**Revive uses a one-shot logic flag, not a pool-level slot skip.** After paying gold to revive, the next spin gets one bomb-free guarantee via `forceNoBombNextSpin` on `WheelLogic`. If RNG lands on the bomb slot, we redirect to a neighbouring slice. The bomb slice is still visually on the wheel for that one spin. A pool-level skip would have meant rebuilding the slice list mid-flow; the logic-level guard was the smaller, safer change. The flag clears itself after a single spin.

**MetaProgress rows are not pooled.** Built once when the panel opens. There are only a handful; pooling would add complexity without a real win.

**No SafeArea handling.** The brief targets landscape only, and Canvas Scaler Expand covers the listed aspect ratios.

**PlayerPrefs for persistence.** Enough for what the brief asks. A proper save file would be future work.

**Timing note.** I submitted slightly later than planned because I refactored the UI from a code-driven setup into a prefab-based structure late in development. The current shape is closer to how production UI is usually authored.

#### 📸 Screenshots

<p align="center">
  <img src="Docs/Screenshots/aspect_20-9_card.png" alt="20:9 Main Gameplay"/>
  <br/>
  <sub><b>20:9</b> · Main Gameplay</sub>
</p>

<p align="center">
  <img src="Docs/Screenshots/aspect_16-9_card.png" alt="16:9 Death and Revive"/>
  <br/>
  <sub><b>16:9</b> · Death &amp; Revive</sub>
</p>

<p align="center">
  <img src="Docs/Screenshots/aspect_4-3_card.png" alt="4:3 Inventory Layout"/>
  <br/>
  <sub><b>4:3</b> · Inventory Layout</sub>
</p>

#### 🚀 Build and Release

- `Tools → Build → Android APK` or `Tools → Build → Android APK + Run`
- Bundle id: `com.simay.vertigowheel` &nbsp;·&nbsp; AndroidMinSdkVersion: 22 &nbsp;·&nbsp; Output: `Build/VertigoWheel.apk`
- The APK is shared via a GitHub Release rather than committed to the repo.

<div align="center">

<a href="https://drive.google.com/file/d/1VxuD5v-L_xG7tuDoB4XAF-BY3kkhfsjW/view?usp=sharing"><img alt="Download APK" src="https://img.shields.io/badge/Download-APK-D4799F?style=for-the-badge&logo=android&logoColor=white&labelColor=1F1F23"/></a>

</div>
