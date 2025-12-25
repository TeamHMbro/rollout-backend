Ручное API-тестирование всех эндпоинтов Rollout Backend (Auth/Events/Chat/Notifications)
Цель

Проверить, что все публичные и защищённые эндпоинты работают корректно:

без токена (где разрешено)

с валидным accessToken (JWT) из auth-service

с неверным/просроченным токеном

проверка статус-кодов, валидации входных данных, пагинации, ошибок

Окружение

Base URL: http://85.198.89.118

Сервисы за nginx:

Auth: /auth/*

Events: /events/*

Chat: /chat/* (если есть REST) + /hubs/chat (SignalR)

Notifications: /notifications/*

Инструменты: Postman / Insomnia / curl

Время: фиксируй дату/время теста (важно для JWT exp/nbf)

Тестовые данные

Пользователь 1

email: t@gmail.com

phone: +7

password: t

A. Smoke + доступность сервисов
A1. Проверка, что сервисы отвечают

Открыть Swagger (если включён):

/auth/swagger

/events/swagger

/chat/swagger

/notifications/swagger
Ожидание: 200/страница открывается.

Health endpoints (если есть):

/auth/health

/events/health

/chat/health

/notifications/health
Ожидание: 200 + статус OK.
Если нет health — зафиксировать как “не реализовано”.

B. Auth-service
B1. Login (получение токенов)

POST /auth/login
Body:

{
  "email": "t@gmail.com",
  "phone": "+7",
  "password": "t"
}


Ожидание:

200

accessToken — строка JWT вида xxx.yyy.zzz (две точки)

refreshToken — строка без точек

Сохранить accessToken и refreshToken для следующих шагов.

B2. Get current user

GET /auth/me
Header: Authorization: Bearer <accessToken>
Ожидание: 200, возвращает id/email/phone.

B3. Неверный токен

GET /auth/me
Header: Authorization: Bearer invalid
Ожидание: 401, WWW-Authenticate содержит ошибку Bearer.

B4. Нет токена

GET /auth/me без Authorization
Ожидание: 401.

(Если есть endpoints refresh/logout/register — прогнать аналогично: валидные/невалидные кейсы.)

C. Events-service
Общие проверки авторизации

Для каждого защищённого эндпоинта:

без токена → 401

с валидным токеном → ожидаемый успех/валидация

с refreshToken вместо accessToken → 401 (важно!)

C1. Создание события

POST /events
Headers:

Authorization: Bearer <accessToken>

Content-Type: application/json

Body (пример):

{
  "title": "Test event",
  "description": "Manual QA",
  "type": 0,
  "city": "Almaty",
  "address": "Somewhere",
  "visibility": 0,
  "maxMembers": 10,
  "price": 0,
  "payment": 0,
  "eventStartAt": "2025-12-31T10:00:00.000Z",
  "eventEndAt": "2025-12-31T12:00:00.000Z",
  "isRecurring": false,
  "recurrenceRule": "",
  "callLink": ""
}


Ожидание:

200/201 (зафиксировать фактическое)

В ответе есть идентификатор события (eventId/ id)

Сохранить eventId.

C2. Валидация входных данных

Повторить C1, но:

пустой title

eventEndAt < eventStartAt

maxMembers отрицательный
Ожидание: 400 + понятная ошибка валидации.

C3. Получение события

GET /events/{eventId}
Ожидание: 200, совпадают поля.

C4. Лента/поиск (пагинация)

GET /events/feed?page=1&pageSize=10 (если есть)
Ожидание: 200, структура списка корректна, page/pageSize/total (если реализовано).

C5. Доступ без токена (если для части ручек разрешён)

Проверить публичные эндпоинты из Swagger (например, /events/feed, /events/{id}):

без токена
Ожидание: 200 (или 401 — тогда это не публичное, зафиксировать).

D. Notifications-service
D1. Получение уведомлений пользователя

GET /notifications?onlyUnread=false&page=1&pageSize=10
Header: Authorization: Bearer <accessToken>
Ожидание: 200, возвращает список (может быть пустой).

Важно: если будет 500 — прикладывать тело ответа и время. Это ловит проблему несовпадения схемы БД.

D2. onlyUnread=true

GET /notifications?onlyUnread=true&page=1&pageSize=10
Ожидание: 200.

D3. Без токена

GET /notifications?... без Authorization
Ожидание: 401.

D4. Внутренний эндпоинт (если доступен извне)

Если есть POST /notifications/internal/notifications:

проверить без токена → 401 (или 403)

с токеном → 200/201
Ожидание: поведение соответствует задумке (если “internal”, внешняя доступность считается дефектом безопасности).

E. Chat-service
E1. REST endpoints (если есть)

Из Swagger:

получить/создать чат/сообщение (по списку)
Для каждого: без токена → 401, с токеном → 200/201.

E2. SignalR (если используется)

Подключение к хабу /hubs/chat:

без токена → 401

с токеном в query ?access_token=... → успешное подключение
Ожидание: handshake успешный, можно отправить тестовое сообщение (если метод есть).

(Если тестеру неудобно — это можно сделать отдельным тестом через небольшой JS-скрипт, но тогда в задаче приложить инструкцию/скрипт.)

F. Негативные сценарии по JWT (межсервисно)
F1. Подмена access → refresh

В любой защищённый endpoint (events/notifications/chat) отправить:
Authorization: Bearer <refreshToken>
Ожидание: 401.

F2. Испорченный токен

Из accessToken удалить 2–3 символа.
Ожидание: 401 + invalid_token.

F3. Токен другого пользователя (если есть 2-й тестовый аккаунт)

Логин под User2, взять token2, дергать данные user1 (если есть такие ручки).
Ожидание: 403 или пусто/ошибка доступа — в зависимости от бизнес-правил.

G. Критерии приёмки

Все эндпоинты из Swagger отвечают ожидаемыми статусами.

JWT из auth-service принимается всеми сервисами (events/chat/notifications).

Нет 500 на “базовых” ручках (/events, /notifications).

Валидация входных данных возвращает 400 с понятными сообщениями.

Пагинация работает стабильно (нет отрицательных offset, page=0 корректно обработан или 400).

Формат отчёта тестера

Для каждого кейса:

Endpoint + method

Request (headers + body)

Response status + body

Фактический результат vs ожидаемый

При ошибках: время, traceId (если есть), скрин/лог, шаги воспроизведения