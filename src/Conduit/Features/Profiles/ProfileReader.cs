using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Profiles
{
    public class ProfileReader : IProfileReader
    {
        private readonly ConduitContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ProfileReader(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProfileEnvelope> ReadProfile(string username)
        {
            var currentUserName = _currentUserAccessor.GetCurrentUsername();

            var person = await _context.Persons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == username);

            if (person == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }
            var profile = Mapper.Map<Domain.Person, Profile>(person);

            if (currentUserName != null)
            {
                var currentPerson = await _context.Persons
                    .Include(x => x.Following)
                    .Include(x => x.Followers)
                    .FirstOrDefaultAsync(x => x.Username == currentUserName);

                if (currentPerson.Followers.Any(x => x.TargetId == person.PersonId))
                {
                    profile.IsFollowed = true;
                }
            }
            return new ProfileEnvelope(profile);
        }
    }
}