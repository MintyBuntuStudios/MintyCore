﻿using ENet;

namespace MintyCore.Network;

/// <summary>
/// How a packet should be delivered. The values are equal to <see cref="ENet.PacketFlags"/>, a cast conversion is possible
/// </summary>
public enum DeliveryMethod
{
    /// <summary>
    /// Send the packet unreliable sequenced, delivery is not guaranteed
    /// </summary>
    UNRELIABLE = 0,

    /// <summary>
    /// Send the packet reliable and in order
    /// </summary>
    RELIABLE = PacketFlags.Reliable,

    /// <summary>
    /// Send the packet unsequenced, may delivered out of order and unreliable
    /// </summary>
    UNSEQUENCED = PacketFlags.Unsequenced,

    /// <summary>
    /// Send the packet as unreliable fragmented. By default large packages will be send automatically as reliable fragment
    /// </summary>
    UNRELIABLE_FRAGMENT = PacketFlags.UnreliableFragmented
}