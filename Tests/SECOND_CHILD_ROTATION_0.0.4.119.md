# PartSculptor 0.0.4.119 — double rotation of the attached child

This release is rebased **directly on 0.0.4.113**, not on 0.0.4.118.
Only live **Copy attached morph steps** gains joint-pose correction; the existing
parent and child morph parameters and the Match mode must remain unchanged.

## Two FL-T400 tanks

1. In the VAB, stack two stock FL-T400 tanks vertically with no offsets.
2. Enable **Copy attached morph steps**. Turn off **Match attached faces** to
   isolate the new live-copy behavior.
3. Select the *lower* tank's upper face and Alt-drag its right corner downward.
4. Verify: its upper-right corner moves down; the upper tank's lower-right
   corner morphs up **in its own local coordinates**; the entire upper tank
   pivots again after the child's face change. The connection node centers
   stay together and the two joint faces remain correctly oriented.
5. Drag the same corner *up* past its original position and reverse again.
   There should be no accumulating rotation, separation, or sign reversal.
6. Undo (Ctrl+Z), redo if available, and cancel a new drag with Esc. The upper
   tank should recover its original pose and morph parameters each time.
7. Repeat the above after rolling the upper tank 90°, 180°, and 270° and
   verify the same visible side remains paired.

## Child-driven morph

1. Start with two unmorphed FL-T400s, Copy enabled.
2. Edit the *upper* tank's bottom face instead.
3. Check the lower tank's corresponding top face receives the reversed local
   tilt, while the **upper tank body** pivots to follow the now-morphed joint.
   The lower/parent tank must not rotate or translate.
4. Repeat in the opposite tilt direction.

## Subtrees, other modes, and persistence

1. Add a pod above the upper FL-T400. The pod should follow the upper tank as
   a rigid assembly when the lower tank's upper face is tilted.
2. Make a roll/tilt, then change to **Match attached faces** with Copy disabled.
   The old geometric-fit behavior must remain unchanged.
3. Close and reopen the editor, save/load craft and switch to flight. No
   additional joint pose correction should be applied merely by loading.
4. Check a pre-tilted or offset joint: original intentional node offset and
   relative node-normal angle should remain the same through live edits.

The offline regressions cover the two-normal math and source code sequencing;
this guide is required because Unity/KSP editor transform behavior cannot be
verified without the game.
