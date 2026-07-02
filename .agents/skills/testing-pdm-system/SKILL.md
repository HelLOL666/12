---
name: testing-pdm-system
description: Test the PDM document archive system end-to-end. Use when verifying login, document upload, viewer, comments, user management, or audit log functionality.
---

# Testing PDM System

## Overview
The PDM System is a document archive web app with ASP.NET Core 9 backend + vanilla JS frontend, deployed via Docker Compose.

## Setup

```bash
cd /home/ubuntu/repos/project-12
docker compose up --build -d
# Wait ~30s for backend to start and run migrations
docker compose ps  # Verify all 3 containers are running
```

## Services
- **Frontend**: http://localhost (Nginx on port 80)
- **Backend API**: http://localhost/api/ (proxied through Nginx)
- **PostgreSQL**: localhost:5432 (user: docarchive, pass: docarchive_pass, db: docarchive)

## Test Credentials
- Username: `admin`
- Password: `Admin123!`
- This user has all permissions (View, Download, Upload, Delete, ManageUsers)

## Key Test Flows

### 1. Login
- Navigate to http://localhost
- Enter admin / Admin123!
- Click "Войти"
- Verify: Documents page loads, nav shows all links, localStorage has `pdm-access-token` starting with `eyJ`

### 2. Document Upload
- Click "Добавить" on Documents page
- Fill number, title, select PDF file
- Click "Загрузить"
- Verify: Toast "Документ загружен", document appears in table with version "v1"

### 3. Document Viewer
- Click "Открыть" on a document row
- Verify: Info panel shows title, number, version, author="System Administrator"
- Tabs: Просмотр (PDF), Версии, Замечания
- Buttons: Скачать, Удалить (admin only)

### 4. Comments
- In viewer, click "Замечания" tab
- Type text in textarea, click "Отправить"
- Verify: Comment appears with author and timestamp, toast "Замечание добавлено"

### 5. User Management
- Navigate to "Пользователи"
- Click "Создать", fill form, submit
- Verify: New user in table, toast "Пользователь создан"

### 6. Audit Log
- Navigate to "Журнал"
- Verify: Table shows actions (Login, UploadDocument, ViewDocument, CreateUser) with timestamps and IPs

### 7. Logout
- Click "Выход"
- Verify: Login screen shown, localStorage token is null

## Tips
- Cyrillic input via `computer` tool may not work reliably. Use `browser_console` to set input values with JavaScript instead.
- The "Открыть" button in the document table might be hard to click precisely — use `browser_console` to find and click it programmatically.
- Dark/light theme persists across sessions via localStorage. Both themes work identically.
- The backend auto-seeds an admin user on first startup if the Users table is empty.
- File uploads accept: PDF, CDW, SPW, M3D, DXF. Use DataTransfer API to set files programmatically in tests.

## Devin Secrets Needed
None required — the system uses local Docker containers with hardcoded development credentials.

## Troubleshooting
- If backend fails to start: check `docker compose logs backend` — common issue is PostgreSQL not ready yet (retry after a few seconds)
- If frontend returns 502: backend hasn't finished starting, wait and retry
- If login fails: verify backend is running with `curl -X POST http://localhost/api/auth/login -H "Content-Type: application/json" -d '{"username":"admin","password":"Admin123!"}'`
