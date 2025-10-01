# BirthdayBuddyLeftChat

## Подготовка СЕ-container в ProxMox
Установим способ входа под root правами временно.

```bash
nano /etc/ssh/sshd_config
```

Должны быть раскоментированы следующие строки:

```bash
Include /etc/ssh/sshd_config.d/*.conf
PermitRootLogin yes
PubkeyAuthentication yes
PasswordAuthentication yes
KbdInteractiveAuthentication no
UsePAM yes
X11Forwarding yes
PrintMotd no
AcceptEnv LANG LC_*
Subsystem       sftp    /usr/lib/openssh/sftp-server
```

Далее перезагружаемся:

```bash
reboot
```

Обновляем репозиторий:

```bash
sudo apt-get update
```

Устанавливаем зависимости:

```bash
sudo apt-get install -y dotnet-sdk-8.0

sudo apt-get install -y aspnetcore-runtime-8.0
```

Проверяем установленную версию dotnet:

```bash
dotnet --version

dotnet --list-sdks
```

Копируем настройки запуска сервиса ```BirthdayBuddyLeftChat.service``` по пути ```/etc/systemd/system/BirthdayBuddyLeftChat.service```

Обновляем сервисы:

```bash
systemctl daemon-reload
```

Создаем папку ```/root/publish/BirthdayBuddyLeftChat```

Ну и собственно можно сделать **Publish**

## Настройка даты на сервере

Подробную дату и часовой пояс можно узнать так:

```bash
timedatectl
```

Пример вывода:

```bash
               Local time: Пн 2025-05-20 15:47:32 MSK
           Universal time: Пн 2025-05-20 12:47:32 UTC
                 RTC time: Пн 2025-05-20 12:47:32
                Time zone: Europe/Moscow (MSK, +0300)
System clock synchronized: yes
              NTP service: active
          RTC in local TZ: no
```

Если все не то - устанавливаем часовой пояс:

```bash
timedatectl set-timezone Europe/Moscow
```
# Проверим есть ли доступ по SSH
```bash
ssh root@telegram.local
```
система спросит, можно ли доверять этому хосту. Нужно полностью написать yes.

## ✅ Что делать если Ты **не создавал SSH-ключ**? Шаг 1: Создать SSH-ключ на Windows

Нужно **создать пару ключей** (приватный + публичный) с помощью `ssh-keygen`.

### 🔧 Выполни в PowerShell:

```powershell
# Создаём директорию .ssh, если её нет
mkdir -p $env:USERPROFILE\.ssh

# Переходим туда
cd $env:USERPROFILE\.ssh

# Генерируем ключ
ssh-keygen -t rsa -b 4096 -C "$env:USERPROFILE@telegrambot" -f id_rsa
```

---

### 📝 Пояснение параметров:

| Флаг | Значение |
|------|--------|
| `-t rsa` | Тип ключа — RSA (поддерживается везде) |
| `-b 4096` | Длина ключа — 4096 бит (надёжно) |
| `-C "..."` | Комментарий (твой email или описание) |
| `-f id_rsa` | Имя файла (`id_rsa` — стандартное) |
| `-N ""` | Пустой пароль (чтобы не спрашивал при каждом использовании) |

> ⚠️ Если не указать `-N ""`, система попросит ввести **пароль для ключа** — тогда каждый раз придётся его вводить. Но в Win11 нужно без этой опции.

---

### ✅ После выполнения будут созданы два файла:

1. `C:\Users\Ludmila\.ssh\id_rsa` — 🔐 **приватный ключ** (хранится только у тебя!)
2. `C:\Users\Ludmila\.ssh\id_rsa.pub` — 🌐 **публичный ключ** (его нужно отправить на сервер)

---

## 🔍 Как посмотреть публичный ключ?

```powershell
type $env:USERPROFILE\.ssh\id_rsa.pub
```

или

```powershell
Get-Content $env:USERPROFILE\.ssh\id_rsa.pub
```

Вывод будет примерно такой:

```
ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQC7... user@telegrambot
```

👉 Это и есть **публичный ключ**, который нужно добавить на Ubuntu-сервер.

---

## 🖥️ Шаг 2: Отправь публичный ключ на сервер Ubuntu

Выполни в PowerShell:

```powershell
type $env:USERPROFILE\.ssh\id_rsa.pub | ssh root@telegram.local "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys && chmod 700 ~/.ssh && chmod 600 ~/.ssh/authorized_keys"
```

> При первом подключении запросит пароль пользователя `root` — введи его.

✅ После этого:
- Твой публичный ключ добавлен в `~/.ssh/authorized_keys`
- Можно заходить без пароля

Ну или можно в ручную скопировать из ```$env:USERPROFILE\.ssh\id_rsa.pub``` ключ в ```~/.ssh/authorized_keys``` вставив его просто следующей строкой без знаков препинания.

---

## 🧪 Шаг 3: Проверь подключение

```powershell
ssh root@telegram.local
```

✅ Теперь должно зайти **без запроса пароля**!

Если всё ещё запрашивает — возможно, нужно перезапустить SSH-агент:

```powershell
# Перезапусти агент (если используется)
Get-Service ssh-agent | Restart-Service
```

---

## 🛠️ Ну и собственно можно теперь сделать **Publish**

Сделаем **автозапуск** сервиса
```bash
systemctl enable BirthdayBuddyLeftChat.service
```

# Настроика автоматической сборки и публикации C# проектов на сервере Ubuntu при коммите в GitHub с помощью CI/CD.

Для управления несколькими проектами и использования общих self-hosted runners в GitHub придется создать свою организацию. На персональных репозиториях (user repos) GitHub не показывает интерфейс для добавления self-hosted runners по умолчанию. Да можно конечно настроить отдельно для каждого репозитория свой runner, но это не удобно и по-этому не наш путь.

## Бесплатная организация на GitHub
Да, можно создать организацию бесплатно, даже с self-hosted runners:

+ Заходи: https://github.com/organizations
+ Нажимай ```Create organization```
+ Выбирай бесплатный план

Там ты получишь:

+ Управление пользователями
+ Совместную работу
+ Общие runners
+ Политики безопасности

Далее потребуется перенести репозиторий из личного аккаунта в организацию.
Это лучший способ, если ты хочешь, чтобы репозиторий стал частью организации (со всеми ветками, историей, issues, PR и т.д.).

## Перенос репозитория в организацию
+ Зайди в свой личный репозиторий (например, https://github.com/логин/MyProject)
+ Перейди в ```Settings```
+ ролистай вниз до секции ```Danger Zone```
+ Нажми ```Transfer ownership```
+ Введи имя своей организации или выбери из списка доступных
+ Подтверди

✅ После этого:

Репозиторий станет: https://github.com/организация/MyProject
Вся история, ветки, issues — останутся
Теперь ты можешь использовать общие runners, секреты, политики и т.д.

⚠️ Убедись, что у тебя есть права Owner и в личном репозитории, и в организации. 

## Создание отдельного пользователя `runner`

### Шаг 1: Проверь, есть ли уже пользователь `runner` или подобный

```bash
id runner
```

Если видишь что-то вроде:
```
uid=1001(runner) gid=1001(runner) groups=1001(runner)
```
→ пользователь уже есть.

Если:
```
id: ‘runner’: no such user
```
→ нужно создать.

---

### Шаг 2: Создай специального пользователя для runner'а

```bash
adduser --system --shell /bin/bash --gecos 'GitHub Runner' --group --disabled-password --home /home/runner runner
```

> Объяснение флагов:
> - `--system` — системный пользователь (без активации пароля)
> - `--shell /bin/bash` — чтобы можно было входить в оболочку
> - `--gecos` — описание
> - `--group` — создать группу с тем же именем
> - `--disabled-password` — без пароля (входит через SSH или sudo)

---

### Шаг 3: Перейди под этого пользователя

```bash
su - runner
```

Теперь ты внутри сессии пользователя `runner`.

---

### Шаг 4: Скачай и распакуй runner **в домашнюю директорию пользователя**

```bash
# Создаём директорию
mkdir actions-runner && cd actions-runner

# Устанавливаем curl
apt install curl

# Скачиваем GitHub-Runner
curl -o actions-runner-linux-x64-2.328.0.tar.gz -L https://github.com/actions/runner/releases/download/v2.328.0/actions-runner-linux-x64-2.328.0.tar.gz

# Делаем валидацию
echo "01066fad3a2893e63e6ca880ae3a1fad5bf9329d60e77ee15f2b97c148c3cd4e  actions-runner-linux-x64-2.328.0.tar.gz" | shasum -a 256 -c

# Распаковываем
tar xzf ./actions-runner-linux-x64-2.328.0.tar.gz
```

(Замени версию на актуальную, если нужно.)

---

### Шаг 5: Запусти настройку

```bash
./config.sh --url https://github.com/your-org-name --token ТВОЙ_ТОКЕН --name "TelegramBotServer" --labels Ubuntu,x64,dotnet
```

Ранер попросит ввести имя группы ранеров, введи например TelegramBotRunners (она должна быть создана в GitHub в разделе Runner groups)

Еще он попросит указать рабочую директорию, можно просто нажать Ввод, оставить по-умолчанию.

## 🛠️ Как добавить автозапуск (через systemd)

Пока ты всё ещё **от пользователя `runner`**, выполни:

```bash
./svc.sh install
```

Это создаст systemd-сервис, который будет запускаться **от имени пользователя `runner`**.

Затем выйди обратно в **root** или обычного пользователя и управляй службой:

```bash
systemctl start actions.runner.ProgiByPalych.TelegramBotServer.service
systemctl enable actions.runner.ProgiByPalych.TelegramBotServer.service
```

Проверь статус:

```bash
sudo systemctl status actions.runner.ProgiByPalych.TelegramBotServer.service
```

---
