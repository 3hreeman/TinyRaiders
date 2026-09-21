# Augment card presentation

The saved `Prefabs/UI/AugmentCard.prefab` is an editor-authored 292 × 432 card. Its layout-owned root stays still; only the centered `VisualRoot` fans out from the center when a new selection appears. The three cards arrive in a short stagger with a tier-colored medallion burst and glow pulse, then pointer hover enlarges a completed card by 4.5%. The card's `CanvasGroup` prevents choosing or rerolling during arrival. The caller also gates each button with `RevealComplete` and the run's readiness.

`Editor/UI/AugmentCardMigration.Apply(ui)` creates the initial prefab and positions the choice viewport. `ApplyToCurrentScene()` is a manual scene migration wrapper. The build hook belongs after `GothicHudMigration.SkinCanvas` so the card's tier art remains intact. The Gothic skin pass should skip cards with a `VisualRoot` child.

Frame resources are in `Resources/Art/UI/Augment`: silver, gold, and prism transparent-corner PNGs. They are single sprites with 28-pixel nine-slice borders. The prism rim blends cyan, violet, and magenta; its UI sparkle mesh uses those three accents as well. The medallion and glow sprites are separate editable layers. The static Noto Sans KR fonts are included under `Resources/Art/Fonts`.

The builder creates art files only when missing and preserves subsequent artist changes. To hand-edit a frame, keep its 128 × 128 canvas, transparent corner cutouts, 28-pixel sprite border, and dark center. `VisualRoot` contains the actual image and text components, so prefab overrides can adjust sizes and colors without modifying runtime code. The title and description use Best Fit, with a dedicated ability tag between them.
