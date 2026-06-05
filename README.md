<div align="center">

### 👾 Spin Wheel Card Reward Game 🫧

<sub><i>Open cases, collect rewards, and test your luck.</i></sub>

<br/>

<img src="./Docs/Screenshots/gameplay_20-9-new.gif" width="620" alt="Gameplay preview"/>

</div>

<br/>

<sub><b>How to play</b></sub>

<table>
  <tr><th><sub>Action</sub></th><th><sub>Effect</sub></th></tr>
  <tr><td><sub>Tap <b>SPIN</b></sub></td><td><sub>Spins the wheel and picks one of the 8 slots.</sub></td></tr>
  <tr><td><sub>Get a reward</sub></td><td><sub>Adds the reward to the current run.</sub></td></tr>
  <tr><td><sub>Complete a skin card</sub></td><td><sub>Finishes that skin card; extra points go to the general puzzle reward.</sub></td></tr>
  <tr><td><sub>Tap <b>EXIT</b></sub></td><td><sub>Saves the rewards collected in the current run.</sub></td></tr>
  <tr><td><sub>Hit death</sub></td><td><sub>The player either gives up the run or spends gold to continue.</sub></td></tr>
  <tr><td><sub>Give up / restart</sub></td><td><sub>Clears current run rewards and rolls back unfinished skin progress.</sub></td></tr>
  <tr><td><sub>Zone rules</sub></td><td><sub>Every 5th zone is safe, every 30th zone is super.</sub></td></tr>
</table>

<sub><b>Technical notes</b></sub>

<table>
  <tr><td><sub><code>RunSession</code> handles the main run flow.</sub></td></tr>
  <tr><td><sub>Wheel rewards are picked by category quotas, with icon and <code>visualFamily</code> checks to avoid repetitive-looking slots.</sub></td></tr>
  <tr><td><sub>UI panels react to run events instead of calling each other directly.</sub></td></tr>
  <tr><td><sub>Pooled UI objects are used for wheel slices, reward rows, and flying reward icons.</sub></td></tr>
  <tr><td><sub>PrimeTween is used for the wheel spin, reward fly/count animations, panel transitions, and meta-progress card feedback.</sub></td></tr>
</table>

<sub><b>Runtime flow</b></sub>

<pre><sub>
Spin
  |
ZoneTrack
  |
RunSession
  |-- WheelResultPicker -> 8 slots / quota / visualFamily
  |-- WheelController ----> spin finished callback
  |
ApplySpinResult
  |
  |-- death  -> RunDeathHitEvent -> RunExitController
  |                                -> WheelController cleanup
  |
  |-- reward -> RewardFeedbackController
                -> RewardAnimationSequence
                   -> RewardFlyIconPool
                   -> MetaProgressPanel
                   -> RewardListUI
                -> complete callback -> next zone
</sub></pre>

<sub><b>Run events</b></sub>

<pre><sub>
RunSession events
  |-- state changed  -> ZoneTrack, RunExitController
  |-- zone changed   -> ZoneTrack
  |-- currency       -> CurrencyHUD
  |-- pending clear  -> RewardListUI, MetaProgressPanel
  |-- exit overlay   -> RewardListUI, MetaProgressPanel
</sub></pre>

<sub><b>Screenshots</b></sub>

<br/><br/>

<p align="center">
  <img src="./Docs/Screenshots/aspect_20-9.png" width="620" alt="20:9 Main Gameplay"/>
  <br/>
  <sub><code>20:9</code> &nbsp; Main gameplay</sub>
</p>

<p align="center">
  <img src="./Docs/Screenshots/aspect_16-9.png" width="620" alt="16:9 Skin Progress"/>
  <br/>
  <sub><code>16:9</code> &nbsp; Skin progress and reward wheel</sub>
</p>

<p align="center">
  <img src="./Docs/Screenshots/aspect_4-3.png" width="520" alt="4:3 Converted Skin Progress"/>
  <br/>
  <sub><code>4:3</code> &nbsp; Completed items convert into puzzle progress</sub>
</p>

<br/>

<sub><b>Project setup</b></sub>

<table>
  <tr>
    <td><sub>Unity</sub></td>
    <td><sub><code>2021.3.45f2 LTS</code></sub></td>
  </tr>
  <tr>
    <td><sub>Scene</sub></td>
    <td><sub><code>Assets/Scenes/SampleScene.unity</code></sub></td>
  </tr>
  <tr>
    <td><sub>Packages</sub></td>
    <td><sub><code>PrimeTween</code> · <code>TextMeshPro</code> · <code>UGUI</code></sub></td>
  </tr>
</table>

<sub><b>License</b></sub>

<table>
  <tr>
    <td><sub>This project is proprietary. Unauthorized copying or use of this code is prohibited.</sub></td>
  </tr>
</table>
