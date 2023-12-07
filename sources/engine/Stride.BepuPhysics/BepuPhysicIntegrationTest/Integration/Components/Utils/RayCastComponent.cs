﻿using System;
using BepuPhysicIntegrationTest.Integration.Components.Collisions;
using BepuPhysicIntegrationTest.Integration.Configurations;
using BepuPhysicIntegrationTest.Integration.Extensions;
using Silk.NET.OpenGL;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class RayCastComponent : SyncScript
    {
        private BepuConfiguration? _bepuConfig;

        public Vector3 Offset { get; set; } = Vector3.Zero;
        public Vector3 Dir { get; set; } = Vector3.UnitZ;
        public float MaxT { get; set; } = 100;


        public override void Start()
        {
            _bepuConfig = Services.GetService<BepuConfiguration>();
        }

        public override void Update() //maybe it would be a better idea to do that in SimulationUpdate since the result will not change without simupdate.
        {
            if (_bepuConfig == null)
                return;

            Entity.Transform.GetWorldTransformation(out var position, out var rotation, out var scale);
            var worldDir = Dir;
            rotation.Rotate(ref worldDir);
            var r = _bepuConfig.BepuSimulations[0].RayCast(Entity.Transform.GetWorldPos() + Offset, worldDir, MaxT);
            DebugText.Print($"hit : {r.Hit}  |  T : {r.T}  |  normal : {r.Normal}  |  col : {r.Collidable} (worldDir : {worldDir})", new((int)(BepuAndStrideExtensions.X_DEBUG_TEXT_POS / 1.3f), 830));
        }
    }

}
