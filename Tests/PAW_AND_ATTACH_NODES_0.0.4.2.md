# PartSculptor 0.0.4.2 — PAW and attach node checks

1. Build with the **Debug** configuration, launch KSP, open a part's PAW. The **Stretch Scale** group should start **expanded**.
2. Build with the **Release** configuration and repeat. The group should start **collapsed** but remain user-expandable.
3. Add a 1.25 m stack tank with top and bottom nodes of size 1. Scale `X=2`, `Y=1`, `Z=2`; their size should become 2. Return to `X=Y=Z=1`; both return to size 1.
4. Scale only `Y=2` on a vertical tank: top/bottom node positions move, but their sizes should remain unchanged.
5. Set a part with a size-0 stack node to uniform `2x`: the node should become size 1. Restore uniform `1x`: it returns to size 0.
6. Check a side-facing attach node on a part with non-uniform scaling, plus surface-attach nodes.
7. Change scaling repeatedly, use Reset scale and Swap X/Z, duplicate via symmetry, save/reload, and inspect child parts (with Stretch children on).
8. With TweakScale installed, test a uniform TweakScale resize then PartSculptor. Node sizes should be calculated from prefab and the combined effective size, without accumulating.

**Important:** This test plan is not an in-game test result. Attach-node sizes are KSP integer tiers, so fractional cross-sectional changes need not cause a tier change.
