# Modular Segmented SRBs exclusion — in-game verification (0.0.4.99)

1. Install ModularSegmentedSRBs and PartSculptor, then restart KSP and let ModuleManager rebuild its cache.
2. Inspect `ModuleManager.ConfigCache` for `PART` nodes corresponding to ModularSegmentedSRBs, including segments, all engines, nosecones, and endcaps. None should contain a `MODULE` named `ModulePartSculptor`.
3. Inspect a stock fuel tank and a stock command pod. Both should still receive `ModulePartSculptor`, showing that installing ModularSegmentedSRBs did not globally disable injection.
4. Inspect a `MODEL` node for each ModularSegmentedSRBs part. Its `model` value should begin `ModularSegmentedSRBs/`; parts that use `mesh =` or a model in another mod's folder are not covered by the model-path filter, and require an explicit part-name exclusion.
5. Verify parts containing `ProceduralPart`, `WingProcedural`, `SmartTankPart`, `DiameterMatcher`, or `TextureDefaulter` are still skipped.
6. KSP and ModuleManager runtime verification was not available when packaging this source.
