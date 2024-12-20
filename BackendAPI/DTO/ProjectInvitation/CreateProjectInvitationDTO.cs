namespace BackendAPI.DTO.ProjectInvitation
{
    public class CreateProjectInvitationDTO
    {
        public int? ProjectId { get; set; }
        public int? UsuarioCreadorId { get; set; }
        public int? UsuarioInvitadoId { get; set; }
    }
}
