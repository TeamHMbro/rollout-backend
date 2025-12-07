using Events.Domain;

namespace Events.Domain.Events;

public class EventEntity
{
    public long Id { get; private set; }
    public Guid OwnerId { get; private set; }

    public string Title { get; private set; }
    public string? Description { get; private set; }

    public EventType Type { get; private set; }

    public string City { get; private set; }
    public string Address { get; private set; }

    public EventVisibility Visibility { get; private set; }
    public EventStatus Status { get; private set; }

    public int? MaxMembers { get; private set; }
    public int MembersCount { get; private set; }

    public int? Price { get; private set; }
    public Payment? Payment { get; private set; }

    public DateTimeOffset EventStartAt { get; private set; }
    public DateTimeOffset? EventEndAt { get; private set; }
    public DateTimeOffset PostDate { get; private set; }

    public bool IsRecurring { get; private set; }
    public string? RecurrenceRule { get; private set; }

    public string? CallLink { get; private set; }

    public int LikesCount { get; private set; }
    public int ViewCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private EventEntity() { }

    public EventEntity(
        Guid ownerId,
        string title,
        string? description,
        EventType type,
        string city,
        string address,
        EventVisibility visibility,
        int? maxMembers,
        int? price,
        Payment? payment,
        DateTimeOffset eventStartAt,
        DateTimeOffset? eventEndAt,
        bool isRecurring,
        string? recurrenceRule,
        string? callLink,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Event.TitleRequired");

        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("Event.CityRequired");

        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Event.AddressRequired");

        if (eventStartAt <= now)
            throw new DomainException("Event.StartMustBeInFuture");

        OwnerId = ownerId;
        Title = title;
        Description = description;
        Type = type;
        City = city;
        Address = address;
        Visibility = visibility;
        Status = EventStatus.Published;
        MaxMembers = maxMembers;
        Price = price;
        Payment = payment;
        EventStartAt = eventStartAt;
        EventEndAt = eventEndAt;
        PostDate = now;
        IsRecurring = isRecurring;
        RecurrenceRule = recurrenceRule;
        CallLink = callLink;
        LikesCount = 0;
        ViewCount = 0;
        MembersCount = 0;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void IncrementMembers(DateTimeOffset now)
    {
        if (MaxMembers.HasValue && MembersCount >= MaxMembers.Value)
            throw new DomainException("Event.MaxMembersReached");

        MembersCount++;
        UpdatedAt = now;
    }

    public void DecrementMembers(DateTimeOffset now)
    {
        if (MembersCount > 0)
        {
            MembersCount--;
            UpdatedAt = now;
        }
    }

    public bool IsActiveForJoin(DateTimeOffset now)
    {
        if (Status != EventStatus.Published)
            return false;

        if (EventStartAt <= now)
            return false;

        if (MaxMembers.HasValue && MembersCount >= MaxMembers.Value)
            return false;

        return true;
    }

    public void IncrementLikes(DateTimeOffset now)
    {
        LikesCount++;
        UpdatedAt = now;
    }

    public void DecrementLikes(DateTimeOffset now)
    {
        if (LikesCount > 0)
        {
            LikesCount--;
            UpdatedAt = now;
        }
    }

    public void Update(
        string? title,
        string? description,
        string? city,
        string? address,
        int? maxMembers,
        int? price,
        Payment? payment,
        DateTimeOffset? eventStartAt,
        DateTimeOffset? eventEndAt,
        bool? isRecurring,
        string? recurrenceRule,
        string? callLink,
        EventVisibility? visibility,
        DateTimeOffset now)
    {
        if (title is not null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Event.TitleRequired");
            Title = title;
        }

        if (description is not null)
            Description = description;

        if (city is not null)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new DomainException("Event.CityRequired");
            City = city;
        }

        if (address is not null)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new DomainException("Event.AddressRequired");
            Address = address;
        }

        if (maxMembers.HasValue)
            MaxMembers = maxMembers;

        if (price.HasValue)
            Price = price;

        if (payment.HasValue)
            Payment = payment;

        if (eventStartAt.HasValue)
        {
            if (eventStartAt.Value <= now)
                throw new DomainException("Event.StartMustBeInFuture");
            EventStartAt = eventStartAt.Value;
        }

        if (eventEndAt.HasValue)
            EventEndAt = eventEndAt;

        if (isRecurring.HasValue)
            IsRecurring = isRecurring.Value;

        if (recurrenceRule is not null)
            RecurrenceRule = recurrenceRule;

        if (callLink is not null)
            CallLink = callLink;

        if (visibility.HasValue)
            Visibility = visibility.Value;

        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == EventStatus.Cancelled)
            return;

        Status = EventStatus.Cancelled;
        UpdatedAt = now;
    }
}
