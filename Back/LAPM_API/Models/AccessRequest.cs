namespace LAPM_API.Models
{
    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Applied,
        Expired, // <-- NEW: Status for when an admin manually revokes access.
        Revoked,
        Error
    }

    public class AccessRequest
    {
        public int Id { get; set; }
        public string ComputerName { get; set; }
        public string DomainUser { get; set; }
        public string Requestor { get; set; }
        public DateTime RequestTime { get; set; }
        public DateTime? ApprovalTime { get; set; }
        public string? Approver { get; set; }
        public DateTime ExpirationTime { get; set; }
        public RequestStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
