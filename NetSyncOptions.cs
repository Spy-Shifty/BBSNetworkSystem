using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

public class NetSyncOptions {
    public string Name { get; private set; }

    public NetSyncMemberAttribute NetSyncMemberAttribute { get; private set; }


    public NetSyncOptions(string name) {
        Name = name;
    }

    public NetSyncOptions(string name, float lerpSpeed = 1, int accuracy = 2, float jumpThreshold = 0, bool initOnly = false) {
        Name = name;
        NetSyncMemberAttribute = new NetSyncMemberAttribute(lerpSpeed, accuracy, jumpThreshold, initOnly);
    }
}
