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