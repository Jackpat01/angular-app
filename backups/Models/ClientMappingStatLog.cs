// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System;
using MSLC.Portal.MVC.Utility;

namespace MSLC.StatLog.UI.Models
{
    public static class ClientMappingStatLog
    {
        /// <summary>
        /// Converts a client GUID into an internal ID if it exists in the ClientMapping Session Dictionary.
        /// </summary>
        /// <param name="clientID">The client GUID to be turned into internal ID.</param>
        /// <param name="scope">The Scope that the ID relationship is stored under.</param>
        /// <returns>Returns a internal ID if one exists in with the guid under the scope, otherwise returns -1.</returns>
        public static int ToInternalID(string clientID, StatLogScopes scope) => 
            int.TryParse(ClientMapping.GetInternalIdFromClientId(clientID, scope.ToString()), out int result) ? result : -1;

        /// <summary>
        /// Converts a client GUID into an internal ID if it exists in the ClientMapping Session Dictionary.
        /// </summary>
        /// <param name="clientID">The client GUID to be turned into internal ID.</param>
        /// <param name="scope">The Scope that the ID relationship is stored under.</param>
        /// <returns>Returns a internal ID if one exists in with the guid under the scope, otherwise returns null</returns>
        public static Guid? ToInternalIDAsGuid(string clientID, StatLogScopes scope) => 
            Guid.TryParse(ClientMapping.GetInternalIdFromClientId(clientID, scope.ToString()), out Guid result) ? result : (Guid?) null;

        /// <summary>
        /// Converts a client GUID into an internal ID if it exists in the ClientMapping Session Dictionary.
        /// </summary>
        /// <param name="clientID">The client GUID to be turned into internal ID.</param>
        /// <param name="scope">The Scope that the ID relationship is stored under.</param>
        /// <returns>Returns a internal ID if one exists in with the guid under the scope, otherwise returns null</returns>
        public static Guid ToInternalIDAsNonNullGuid(string clientID, StatLogScopes scope) =>
            Guid.TryParse(ClientMapping.GetInternalIdFromClientId(clientID, scope.ToString()), out Guid result) ? result : Guid.Empty;

        /// <summary>
        /// Converts a internal id into a GUID that can be given to a client. The ID relationship is stored under that scope.
        /// </summary>
        /// <param name="internalID">The internal ID turned into a client GUID.</param>
        /// <param name="scope">The scope that the ID relationship will be stored under.</param>
        /// <returns>Returns the client GUID that pairs with the given internal ID under the given scope.</returns>
        public static string ToClientID(int? internalID, StatLogScopes scope) => 
            ClientMapping.MapInternalIdToClientId(internalID?.ToString(), scope.ToString());

        /// <summary>
        /// Converts a string into a GUID that can be given to a client. The ID relationship is stored under that scope.
        /// </summary>
        /// <param name="internalID">The internal string turned into a client GUID.</param>
        /// <param name="scope">The scope that the ID relationship will be stored under.</param>
        /// <returns>Returns the client GUID that pairs with the given internal string under the given scope.</returns>
        public static string ToClientID(string internalID, StatLogScopes scope) => 
            ClientMapping.MapInternalIdToClientId(internalID, scope.ToString());
    }

    public enum StatLogScopes
    {
        PROVIDER_ID_STATLOG,
        COST_REPORT_ID_STATLOG,
        HISTORY_ID_STATLOG,
        EVENT_ID_STATLOG,
        ASSIGNED_STATE_USER_STATLOG,
        FILE_ID_STATLOG,
        ROLE_ID_STATLOG,
        FILE_TEMPLATE_ID_STATLOG,
        CORRESPONDENT_ID_STATLOG,
        EVENT_ID_MANAGEMENT,
        FILE_ID_MANAGEMENT,
        ROLE_ID_MANAGEMENT,
        WORKFLOW_PK_MANAGEMENT,
        WORKFLOW_EVENT_MANAGEMENT,
        ASSIGN_USER_ROLE_ID_EVENT
    }
}