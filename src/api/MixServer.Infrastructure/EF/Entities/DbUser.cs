using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Models;

namespace MixServer.Infrastructure.EF.Entities;

public class DbUser : IdentityUser, IUser
{
    public bool PasswordResetRequired { get; set; }

    public PlaybackSession? CurrentPlaybackSession { get; set; }

    public List<PlaybackSession> PlaybackSessions { get; set; } = [];

    public List<FolderSort> FolderSorts { get; set; } = [];

    public List<Device> Devices { get; set; } = [];

    public List<UserCredential> Credentials { get; set; } = [];
    
    public QueueEntity? Queue { get; set; }
    
    [NotMapped]
    public IList<Role> Roles { get; set; } = new List<Role>();

    public IFolderSort GetSortOrDefault(NodePath nodePath)
    {
        var sort = FolderSorts.SingleOrDefault(s => s.NodeEntity.Path.IsEqualTo(nodePath));

        if (sort == null)
        {
            return FolderSortModel.Default;
        }

        return sort;
    }

    public bool InRole(Role role)
    {
        return Roles.Contains(role);
    }
    
    public bool IsAdminOrOwner()
    {
        return InRole(Role.Administrator) || InRole(Role.Owner);
    }
}