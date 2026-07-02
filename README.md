# PDM System — Система хранения конструкторской документации

Веб-система для хранения, управления и просмотра конструкторской документации предприятия (до 20 пользователей).

## Стек технологий

- **Backend**: ASP.NET Core 9 Web API, Entity Framework Core, PostgreSQL
- **Frontend**: HTML, CSS, JavaScript (без фреймворков)
- **Auth**: JWT Access Token + Refresh Token, Argon2 хеширование паролей
- **Контейнеризация**: Docker, Docker Compose, Nginx

## Быстрый запуск

```bash
docker compose up --build
```

Приложение будет доступно по адресу: http://localhost

### Учётные данные по умолчанию

- **Логин**: `admin`
- **Пароль**: `Admin123!`

## Структура проекта

```
├── src/
│   ├── Backend/              # ASP.NET Core 9 Web API (Clean Architecture)
│   │   ├── DocArchive.API/        # Контроллеры, middleware, конфигурация
│   │   ├── DocArchive.Application/ # DTOs, валидаторы, сервисы, маппинги
│   │   ├── DocArchive.Domain/      # Сущности, перечисления, интерфейсы
│   │   └── DocArchive.Infrastructure/ # EF Core, репозитории, реализации
│   └── Frontend/             # Статический фронтенд (HTML/CSS/JS)
│       ├── index.html
│       ├── css/style.css
│       ├── js/app.js
│       ├── nginx.conf
│       └── Dockerfile
├── docker-compose.yml
└── README.md
```

## Основные возможности

- Загрузка документов (PDF, CDW, SPW, M3D, DXF)
- Версионирование документов
- Просмотр PDF в браузере
- Поиск, сортировка, пагинация
- Управление пользователями и ролями
- Разграничение прав доступа (просмотр, скачивание, загрузка, удаление, управление)
- Замечания к документам
- Журнал действий (аудит)
- Светлая / тёмная тема
- Адаптивный дизайн (desktop, tablet, mobile)

## API

Swagger UI: http://localhost:5000/swagger (в dev-режиме)

## Безопасность

- JWT + Refresh Token с ротацией
- Argon2 хеширование паролей
- Rate Limiting (10 попыток входа/мин)
- Security Headers (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection)
- Валидация MIME-типов и расширений файлов
- Ограничение размера файлов (200 МБ)
- Проверка прав на каждом API-запросе
- Файлы хранятся вне web-root, доступ только через API

## Конфигурация

| Переменная | Описание |
|---|---|
| `ConnectionStrings__DefaultConnection` | Строка подключения к PostgreSQL |
| `Jwt__Secret` | Секретный ключ для JWT (мин. 32 символа) |
| `Storage__Path` | Путь хранения файлов |
