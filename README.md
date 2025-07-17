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