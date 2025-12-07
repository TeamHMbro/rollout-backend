using System.Collections.Generic;

namespace Notifications.Application.Models;

public sealed record MarkReadRequest(IReadOnlyCollection<long> Ids);
