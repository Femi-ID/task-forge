namespace api.Dtos.WorkspaceInvite
{
    public class WorkspaceInviteResponseDto
    {
        public Guid InviteId { get; set; }
        public Guid WorkspaceId { get; set; }
        public string InvitedEmail { get; set; } = null!;
        public DateTime ExpireAt { get; set; }
        public string RawToken { get; set; } = null!;
    }
}