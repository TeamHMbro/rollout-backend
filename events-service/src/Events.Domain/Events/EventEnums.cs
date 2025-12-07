namespace Events.Domain.Events;

public enum EventType
{
    Offline = 0,
    Online = 1
}

public enum EventVisibility
{
    PublicCity = 0,
    PublicRadius = 1,
    FriendsOnly = 2,
    PrivateLink = 3
}

public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2,
    Completed = 3
}

public enum Payment
{
    Immediately = 0,
    OnTheSpot = 1
}

public enum RsvpStatus
{
    Going = 0,
    Maybe = 1,
    Declined = 2
}

public enum ParticipantRole
{
    Participant = 0,
    Organizer = 1,
    Moderator = 2
}
