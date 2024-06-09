// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
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
public sealed class HingeConstraintComponent : TwoBodyConstraintComponent<Hinge>
{
    public HingeConstraintComponent() => BepuConstraint = new()
    {
        SpringSettings = new SpringSettings(30, 5)
    };

    public Vector3 LocalOffsetA
    {
        get
        {
            return BepuConstraint.LocalOffsetA.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetA = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Vector3 LocalHingeAxisA
    {
        get
        {
            return BepuConstraint.LocalHingeAxisA.ToStride();
        }
        set
        {
            BepuConstraint.LocalHingeAxisA = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Vector3 LocalOffsetB
    {
        get
        {
            return BepuConstraint.LocalOffsetB.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetB = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Vector3 LocalHingeAxisB
    {
        get
        {
            return BepuConstraint.LocalHingeAxisB.ToStride();
        }
        set
        {
            BepuConstraint.LocalHingeAxisB = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float SpringFrequency
    {
        get
        {
            return BepuConstraint.SpringSettings.Frequency;
        }
        set
        {
            BepuConstraint.SpringSettings.Frequency = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float SpringDampingRatio
    {
        get
        {
            return BepuConstraint.SpringSettings.DampingRatio;
        }
        set
        {
            BepuConstraint.SpringSettings.DampingRatio = value;
            ConstraintData?.TryUpdateDescription();
        }
    }
}
