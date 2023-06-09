﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
public class MinEventActionAddFireDamage : MinEventActionRemoveBuff
{

    private static readonly string AdvFeatureClass = "FireManagement";
    private float delayTime = 0;

    //  		<triggered_effect trigger="onSelfDamagedBlock" action="AddFireDamage, SCore" target="positionAOE" range="5" delayTime="10" />

    public override void Execute(MinEventParams _params)
    {
        if (FireManager.Instance == null) return;
        if (FireManager.Instance.Enabled == false) return;

        var position = _params.Position;
        if (targetType != TargetTypes.positionAOE)
        {
            if (Voxel.voxelRayHitInfo.bHitValid)
            {
                var hitInfo = Voxel.voxelRayHitInfo;
                if (hitInfo == null) return;

                /// modification from FuriousRamsay to fix issue where the lookray is behind a door, which is setting fire to blocks on the other side of metal doors.
                ItemAction itemAction = _params.Self.inventory.holdingItemData.item.Actions[0];
                if (itemAction is ItemActionMelee)
                {
                    float blockRange = 0;
                    if (itemAction.Properties.Values.ContainsKey("Block_range"))
                    {
                        blockRange = StringParsers.ParseFloat(itemAction.Properties.Values["Block_range"], 0, -1, NumberStyles.Any);
                    }
                    if ((blockRange > 0) && (Vector3.Distance(hitInfo.hit.blockPos, _params.Position) > blockRange))
                    {
                        return;
                    }
                }
                position = hitInfo.hit.blockPos;
            }
            //if (Voxel.voxelRayHitInfo.bHitValid)
            //{
            //    var hitInfo = Voxel.voxelRayHitInfo;
            //    if (hitInfo == null) return;
            //    position = hitInfo.hit.blockPos;
            //}
        }

       // AdvLogging.DisplayLog(AdvFeatureClass, $"Executing AddFireDamage() at {position}  Self: {_params.Self.position} Range: {maxRange}  Delay: {delayTime}");
        
        Task task = Task.Delay((int)delayTime)
             .ContinueWith(t => AddFire(position));
    }

    public void AddFire(Vector3 position)
    {
        int range = (int)maxRange;
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                for (int y = -range; y <= range; y++)
                {
                    var vector = new Vector3i(position.x + x, position.y + y, position.z + z);
                    FireManager.Instance.Add(vector);

                }
            }
        }
    }

    public override bool ParseXmlAttribute(XmlAttribute _attribute)
    {
        var flag = base.ParseXmlAttribute(_attribute);
        if (!flag)
        {
            var name = _attribute.Name;
            if (name != null)
            {
                if (name == "delayTime")
                {
                    delayTime = StringParsers.ParseFloat(_attribute.Value);
                    return true;
                }
            }
        }

        return flag;
    }
}