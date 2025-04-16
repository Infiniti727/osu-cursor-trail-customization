// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Osu.Skinning.Legacy
{
    public partial class LegacyCursorTrail : CursorTrail
    {
        private readonly ISkin skin;
        private const double disjoint_trail_time_separation = 1000 / 60.0;

        public bool DisjointTrail { get; private set; }
        private double lastTrailTime;

        private IBindable<float> cursorSize = null!;

        private readonly Bindable<bool> longTrail = new Bindable<bool>(true);

        private readonly Bindable<float> longTrailLength = new Bindable<float>(1.00f);

        private readonly Bindable<float> longTrailUpdateInterval = new Bindable<float>(1.00f);

        private Vector2? currentPosition;

        public LegacyCursorTrail(ISkin skin)
        {
            this.skin = skin;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, ISkinSource skinSource, OsuRulesetConfigManager rulesetConfig)
        {
            cursorSize = config.GetBindable<float>(OsuSetting.GameplayCursorSize).GetBoundCopy();
            AllowPartRotation = skin.GetConfig<OsuSkinConfiguration, bool>(OsuSkinConfiguration.CursorTrailRotate)?.Value ?? true;

            Texture = skin.GetTexture("cursortrail");

            // Cursor and cursor trail components are sourced from potentially different skin sources.
            // Stable always chooses cursor trail disjoint behaviour based on the cursor texture lookup source, so we need to fetch where that occurred.
            // See https://github.com/peppy/osu-stable-reference/blob/3ea48705eb67172c430371dcfc8a16a002ed0d3d/osu!/Graphics/Skinning/SkinManager.cs#L269
            var cursorProvider = skinSource.FindProvider(s => s.GetTexture("cursor") != null);

            rulesetConfig?.BindWith(OsuRulesetSetting.LongCursorTrail, longTrail);
            rulesetConfig?.BindWith(OsuRulesetSetting.LongCursorTrailLength, longTrailLength);
            rulesetConfig?.BindWith(OsuRulesetSetting.LongCursorTrailUpdateInterval, longTrailUpdateInterval);

            //DisjointTrail = cursorProvider?.GetTexture("cursormiddle") == null;

            if (longTrail.Value == false)
            {
                bool centre = skin.GetConfig<OsuSkinConfiguration, bool>(OsuSkinConfiguration.CursorCentre)?.Value ?? true;

                TrailOrigin = centre ? Anchor.Centre : Anchor.TopLeft;
                Blending = BlendingParameters.Inherit;
            }
            else
            {
                Blending = BlendingParameters.Additive;
            }

            if (Texture != null)
            {
                // stable "magic ratio". see OsuPlayfieldAdjustmentContainer for full explanation.
                Texture.ScaleAdjust *= 1.6f;
            }
        }

        protected override double FadeDuration => !longTrail.Value ? 150 : 500 * longTrailLength.Value;
        protected override float FadeExponent => 1;

        protected override bool InterpolateMovements => longTrail.Value;

        //protected override float IntervalMultiplier => 1 / Math.Max(cursorSize.Value, 1);

        protected override float IntervalMultiplier => longTrailUpdateInterval.Value / Math.Max(cursorSize.Value, 1);
        protected override bool AvoidDrawingNearCursor => longTrail.Value;

        protected override void Update()
        {
            base.Update();

            if (longTrail.Value || !currentPosition.HasValue)
                return;

            if (Time.Current - lastTrailTime >= disjoint_trail_time_separation)
            {
                lastTrailTime = Time.Current;
                AddTrail(currentPosition.Value);
            }
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            if (longTrail.Value)
                return base.OnMouseMove(e);

            currentPosition = e.ScreenSpaceMousePosition;

            // Intentionally block the base call as we're adding the trails ourselves.
            return false;
        }
    }
}
