что есть сейчас в MVP и как это предполагается использовать.

Будем считать базовый URL:

BASE_URL = http://85.198.89.118


Все запросы и ответы — application/json, кроме /health.

1. Общие правила
1.1. Авторизация

Формат: JWT в заголовке:

Authorization: Bearer {accessToken}


Без токена доступны только:

POST /auth/register

POST /auth/login

POST /auth/refresh

Все остальные эндпоинты — только с токеном. При отсутствии/просрочке токена получишь 401.

1.2. Формат JSON

Поля в JSON — camelCase: eventId, createdAt, maxMembers и т.д.

2. Auth Service (/auth)

Это всё, что связано с регистрацией, логином и текущим пользователем.

⚠️ Важно: логический контракт такой, как ниже. В текущем деплое есть технический нюанс с двойным /auth/auth, но для новой среды/клиента считаем, что публичный API такой:

2.1. Регистрация

POST BASE_URL/auth/register

Создаёт нового пользователя и сразу логинит (возвращает токены).

Request:

{
  "email": "user@example.com",
  "phone": "+77060000001",
  "password": "Password123!@#"
}


email и phone — один из них обязателен (на бэке это валидируется).

password — обязательный.

Response 200:

{
  "accessToken": "string",
  "refreshToken": "string",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "phone": "+77060000001"
  }
}


Использование на клиенте:

после успешной регистрации сразу сохраняешь accessToken + refreshToken,

переходишь в основной флоу приложения (экран ленты).

2.2. Логин

POST BASE_URL/auth/login

Логин по email/телефону и паролю.

Request:

{
  "email": "user@example.com",
  "phone": "+77060000001",
  "password": "Password123!@#"
}


Можно использовать:

либо email + password,

либо phone + password.

Response 200:

{
  "accessToken": "string",
  "refreshToken": "string",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "phone": "+77060000001"
  }
}

2.3. Обновление токена

POST BASE_URL/auth/refresh

Используется, когда accessToken истёк.

Request:

{
  "refreshToken": "string"
}


Response 200:

{
  "accessToken": "string",
  "refreshToken": "string"
}


На клиенте после 401 по защищённому запросу:

пробуешь refresh,

подменяешь токены,

ретраишь запрос.

2.4. Текущий пользователь

GET BASE_URL/auth/me
Требует Authorization: Bearer

Возвращает базовые данные текущего пользователя.

Response 200:

{
  "id": "uuid",
  "email": "user@example.com",
  "phone": "+77060000001"
}


Использование:

проверка, что токен валиден,

получение id пользователя для дальнейших запросов (например, фильтрация «мои события»).

3. Events Service (/events)

Всё, что связано с событиями: лента, деталка, участие, лайки/сейвы.

3.1. Лента событий

GET BASE_URL/events/feed
Требует Authorization

Показывает события, доступные пользователю (в простом варианте — по городу/по дате).

Пример запроса:

GET /events/feed?city=Astana&page=1&pageSize=20
Authorization: Bearer {token}


Параметры:

city — строка (например, "Astana"),

page, pageSize — пагинация (может быть cursor-based в будущем; пока проще считать page/size).

Response 200:

{
  "items": [
    {
      "id": "uuid",
      "title": "Баскетбол 3x3",
      "description": "Играем 3 команды по 3 человека",
      "city": "Astana",
      "address": "Парк Триатлон, под мостом",
      "eventStartAt": "2025-12-10T18:00:00Z",
      "maxMembers": 9,
      "membersCount": 3,
      "price": 0,
      "status": "Published",
      "liked": false,
      "saved": false,
      "isOwner": false,
      "createdBy": "uuid"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "hasMore": true
}

3.2. Создать событие

POST BASE_URL/events
Требует Authorization

Request:

{
  "title": "Баскетбол 3x3",
  "description": "Играем 3 команды по 3 человека",
  "city": "Astana",
  "address": "Парк Триатлон, под мостом",
  "eventStartAt": "2025-12-10T18:00:00Z",
  "maxMembers": 9,
  "price": 0
}


Response 201:

{
  "id": "uuid",
  "title": "Баскетбол 3x3",
  "description": "Играем 3 команды по 3 человека",
  "city": "Astana",
  "address": "Парк Триатлон, под мостом",
  "eventStartAt": "2025-12-10T18:00:00Z",
  "maxMembers": 9,
  "membersCount": 1,
  "price": 0,
  "status": "Published",
  "createdBy": "uuid"
}


Использование:

экран «создать событие» → после 201 можно редиректить в деталку события.

3.3. Детальная карточка события

GET BASE_URL/events/{eventId}
Authorization: Bearer

Response 200:

{
  "id": "uuid",
  "title": "Баскетбол 3x3",
  "description": "Играем 3 команды по 3 человека",
  "city": "Astana",
  "address": "Парк Триатлон, под мостом",
  "eventStartAt": "2025-12-10T18:00:00Z",
  "maxMembers": 9,
  "membersCount": 3,
  "price": 0,
  "status": "Published",
  "liked": true,
  "saved": false,
  "isOwner": false,
  "createdBy": "uuid",
  "members": [
    {
      "userId": "uuid",
      "displayName": "Al",
      "avatarUrl": "https://..."
    }
  ]
}

3.4. Вступить в событие

POST BASE_URL/events/{eventId}/join
Authorization: Bearer

Без тела.

Response 200:

{
  "membersCount": 4,
  "joined": true
}


Особенности:

На бэке стоит защита: нельзя вступить, если membersCount >= maxMembers или событие не в статусе Published.

В случае, если мест уже нет → 400 или 409, клиент должен показать сообщение «мест нет».

3.5. Выйти из события

DELETE BASE_URL/events/{eventId}/join
Authorization: Bearer

Response 200:

{
  "membersCount": 3,
  "joined": false
}

3.6. Лайк события

POST BASE_URL/events/{eventId}/like
Authorization: Bearer

Помечает событие лайком от текущего пользователя.

Response 200:

{
  "liked": true,
  "likesCount": 10
}


Удалить лайк:

DELETE BASE_URL/events/{eventId}/like

3.7. Сохранить событие

POST BASE_URL/events/{eventId}/save
DELETE BASE_URL/events/{eventId}/save
Authorization: Bearer

Это для экрана «сохранённые события».

3.8. Мои события / мои посещения

Для профиля пользователя удобно иметь:

GET /events/my/created — события, которые пользователь создал.

GET /events/my/joined — события, куда он записан.

GET /events/my/saved — сохранённые.

Формат ответа — такой же, как в /events/feed, только набор другой.

4. Chat Service (/chat)

Чат привязан к событию: у каждого event свой «канал».

4.1. Получить сообщения события

GET BASE_URL/chat/messages
Authorization: Bearer

Query-параметры:

eventId — обязательный:

GET /chat/messages?eventId={uuid}&limit=50&beforeMessageId=...

limit — сколько сообщений за раз.

beforeMessageId или beforeCreatedAt — для пагинации назад (история).

Response 200:

{
  "items": [
    {
      "id": "uuid",
      "eventId": "uuid",
      "authorId": "uuid",
      "authorName": "Al",
      "text": "Встречаемся у входа в парк",
      "createdAt": "2025-12-10T16:00:00Z",
      "editedAt": null
    }
  ],
  "hasMore": true,
  "nextCursor": "opaque-string"
}


Использование:

чат-экран внутри события,

загрузка истории вниз/вверх.

4.2. Отправить сообщение

POST BASE_URL/chat/messages
Authorization: Bearer

Request:

{
  "eventId": "uuid",
  "text": "Приду на 10 минут позже"
}


Response 201:

{
  "id": "uuid",
  "eventId": "uuid",
  "authorId": "uuid",
  "authorName": "Al",
  "text": "Приду на 10 минут позже",
  "createdAt": "2025-12-10T16:05:00Z",
  "editedAt": null
}

4.3. Редактирование / удаление (запланировано)

PATCH /chat/messages/{messageId} — изменить текст (только автор).

DELETE /chat/messages/{messageId} — удалить (мягкое удаление).

Это ещё не реализовано полностью, но доменная модель к этому готова.

5. Notification Service (/notifications)

Внутреннее хранилище уведомлений для пользователя: новые события, изменения, системные сообщения и т.п.

5.1. Список уведомлений

GET BASE_URL/notifications
Authorization: Bearer

Query-параметры:

page, pageSize или cursor — пагинация.

Response 200:

{
  "items": [
    {
      "id": "uuid",
      "type": "EVENT_UPDATED",
      "title": "Изменилось время события",
      "body": "Баскетбол 3x3 теперь начнётся в 19:00",
      "createdAt": "2025-12-10T15:00:00Z",
      "isRead": false,
      "eventId": "uuid"
    }
  ],
  "hasMore": true,
  "nextCursor": "..."
}


Использование:

экран «Уведомления» в приложении.

5.2. Пометить уведомление прочитанным

POST BASE_URL/notifications/{id}/read
Authorization: Bearer

Без тела.

Response 200:

{
  "id": "uuid",
  "isRead": true
}


Также можно сделать батч-вариант:

POST /notifications/read:

{
  "ids": ["uuid1", "uuid2"]
}

5.3. Счётчик непрочитанных

(удобно для бейджа в UI)

GET BASE_URL/notifications/unread-count
Authorization: Bearer

Response 200:

{
  "unreadCount": 5
}

6. Как этим пользоваться на мобилке (сценарно)

Первый запуск

Проверяешь, есть ли сохранённые токены.

Если нет → экран логина/регистрации.

После login/register сохраняешь токены, вызываешь /auth/me для проверки.

Главная лента

GET /events/feed?city=....

По клику на карточку → GET /events/{id}.

Записаться на событие

Кнопка «Пойти» → POST /events/{id}/join.

После 200 обновляешь локальный стейт (joined = true, membersCount++).

Чат события

На вход в чат:

GET /chat/messages?eventId={id}&limit=50.

Отправка:

POST /chat/messages.

Периодический поллинг или (в будущем) WebSocket для новых сообщений.

Уведомления

При открытии экрана:

GET /notifications.

При клике на уведомление:

POST /notifications/{id}/read,

переход к событию / нужному экрану.