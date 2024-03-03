﻿using BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Constraint")]
public sealed class AngularAxisMotorConstraintComponent : TwoBodyConstraintComponent<AngularAxisMotor>
{
    public AngularAxisMotorConstraintComponent() => BepuConstraint = new() { Settings = new MotorSettings(1000, 10) };

    public Vector3 LocalAxisA
    {
        get
        {
            return BepuConstraint.LocalAxisA.ToStride();
        }
        set
        {
            BepuConstraint.LocalAxisA = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float TargetVelocity
    {
        get
        {
            return BepuConstraint.TargetVelocity;
        }
        set
        {
            BepuConstraint.TargetVelocity = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float MotorDamping
    {
        get
        {
            return BepuConstraint.Settings.Damping;
        }
        set
        {
            BepuConstraint.Settings.Damping = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float MotorMaximumForce
    {
        get
        {
            return BepuConstraint.Settings.MaximumForce;
        }
        set
        {
            BepuConstraint.Settings.MaximumForce = value;
            ConstraintData?.TryUpdateDescription();
        }
    }
}
