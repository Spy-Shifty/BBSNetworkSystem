using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class NetworkMessageUtility {
    private const int tab1 = 4;
    private const int tab2 = 8;
    private const int tab3 = 12;

    public static string ToString(NetworkSyncDataContainer networkDataContainer) {

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("NetworkSyncDataEntityContainers: {");
        foreach (NetworkSyncDataEntityContainer networkSyncDataEntityContainer in networkDataContainer.NetworkSyncDataEntities) {
            stringBuilder.AppendLine(string.Format("{0}NetworkSyncEntity: ", new String(' ', tab1))+"{");
            stringBuilder.AppendLine(string.Format("{0}NetworkId: {1}", new String(' ', tab2), networkSyncDataEntityContainer.NetworkSyncEntity.NetworkId));
            stringBuilder.AppendLine(string.Format("{0}ActorId: {1}", new String(' ', tab2), networkSyncDataEntityContainer.NetworkSyncEntity.ActorId));
            stringBuilder.AppendLine(string.Format("{0}", new String(' ', tab1)) + "}");

            stringBuilder.Append(string.Format("{0}AddedComponents: [ ", new String(' ', tab1)));
            if (networkSyncDataEntityContainer.AddedComponents.Any()) {
                stringBuilder.AppendLine();
            }
            foreach (ComponentDataContainer componentDataContainer in networkSyncDataEntityContainer.AddedComponents) {
                stringBuilder.AppendLine(string.Format("{0}componentDataContainer: ", new String(' ', tab2)) + "{");
                stringBuilder.AppendLine(string.Format("{0}ComponentTypeId: {1}", new String(' ', tab3), componentDataContainer.ComponentTypeId));
                stringBuilder.AppendLine(string.Format("{0}MemberData: [ {1} ]", new String(' ', tab3), string.Join(", ", componentDataContainer.MemberData.Select(x => x.Data))));
                stringBuilder.AppendLine(string.Format("{0}", new String(' ', tab2)));
            }
            if (networkSyncDataEntityContainer.AddedComponents.Any()) {
                stringBuilder.Append(new String(' ', tab1));
            }
            stringBuilder.AppendLine("]");

            stringBuilder.AppendLine(string.Format("{0}RemovedComponents: [ {1} ]", new String(' ', tab1), string.Join(", ", networkSyncDataEntityContainer.RemovedComponents)));

            stringBuilder.Append(string.Format("{0}ComponentData: [ ", new String(' ', tab1)));
            if (networkSyncDataEntityContainer.ComponentData.Any()) {
                stringBuilder.AppendLine();
            }
            foreach (ComponentDataContainer componentDataContainer in networkSyncDataEntityContainer.ComponentData) {
                stringBuilder.AppendLine(string.Format("{0}ComponentDataContainer: ", new String(' ', tab2)) + "{");
                stringBuilder.AppendLine(string.Format("{0}ComponentTypeId: {1}", new String(' ', tab3), componentDataContainer.ComponentTypeId));
                stringBuilder.AppendLine(string.Format("{0}MemberData: [ {1} ]", new String(' ', tab3), string.Join(", ", componentDataContainer.MemberData.Select(x=>x.Data))));
                stringBuilder.AppendLine(string.Format("{0}", new String(' ', tab2))+"}");
            }
            if (networkSyncDataEntityContainer.ComponentData.Any()) {
                stringBuilder.Append(new String(' ', tab1));
            }
            stringBuilder.AppendLine("]");
        }
        stringBuilder.AppendLine("}");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("AddedNetworkSyncEntities: {");
        foreach (NetworkEntityData networkEntityData in networkDataContainer.AddedNetworkSyncEntities) {
            stringBuilder.AppendLine(string.Format("{0}NetworkEntityData: ", new String(' ', tab1)) + "{");
            stringBuilder.AppendLine(string.Format("{0}NetworkSyncEntity: ", new String(' ', tab2)) + "{");
            stringBuilder.AppendLine(string.Format("{0}NetworkId: {1}", new String(' ', tab3), networkEntityData.NetworkSyncEntity.NetworkId));
            stringBuilder.AppendLine(string.Format("{0}ActorId: {1}", new String(' ', tab3), networkEntityData.NetworkSyncEntity.ActorId));
            stringBuilder.AppendLine(string.Format("{0}", new String(' ', tab2))+ "}");

            stringBuilder.Append(string.Format("{0}ComponentData: [ ", new String(' ', tab1)));
            if (networkEntityData.ComponentData.Any()) {
                stringBuilder.AppendLine();
            }
            foreach (ComponentDataContainer componentDataContainer in networkEntityData.ComponentData) {
                stringBuilder.AppendLine(string.Format("{0}ComponentDataContainer: ", new String(' ', tab2)) + "{");
                stringBuilder.AppendLine(string.Format("{0}ComponentTypeId: {1}", new String(' ', tab3), componentDataContainer.ComponentTypeId));
                stringBuilder.AppendLine(string.Format("{0}MemberData: [ {1} ]", new String(' ', tab3), string.Join(", ", componentDataContainer.MemberData.Select(x => x.Data))));
                stringBuilder.AppendLine(string.Format("{0}", new String(' ', tab2)) + "}");
            }
            if (networkEntityData.ComponentData.Any()) {
                stringBuilder.Append(new String(' ', tab1));
            }
            stringBuilder.AppendLine("]");
            stringBuilder.AppendLine(new String(' ', tab1) + "}");
        }
        stringBuilder.AppendLine("}");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("AddedNetworkSyncEntities: {");
        foreach (NetworkSyncEntity networkSyncEntity in networkDataContainer.RemovedNetworkSyncEntities) {
            stringBuilder.AppendLine(string.Format("{0}NetworkSyncEntity: ", new String(' ', tab1)) + "{");
            stringBuilder.AppendLine(string.Format("{0}NetworkId: {1}", new String(' ', tab2), networkSyncEntity.NetworkId));
            stringBuilder.AppendLine(string.Format("{0}ActorId: {1}", new String(' ', tab2), networkSyncEntity.ActorId));
            stringBuilder.AppendLine(string.Format("{0}", new String(' ', tab1)) + "}");
        }
        stringBuilder.AppendLine("}");

        return stringBuilder.ToString();
    }
}

