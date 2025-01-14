# MailWave
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)

MailWave - почтовый клиент, который способен работать с 3 почтовыми сервисами, такими как: Gmail, Yandex, Mail.ru. Предоставляет удобную функциональность и централизованный доступ.

<details><summary><h2>Скриншоты</h2></summary>
Будут добавлены, когда будет готова фронтенд часть платформы
</details>

## Возможности backend`а:
- [x] Аутентификация в почтовый клиент по JWT с проверкой соединения по протоколу SMTP/IMAP
- [x] Хранение данных о пользователях и сессиях в MongoDB
- [x] Происходит заполнение данных о учётной записи пользователя с модуля Account в модуль Mail через команду RabbiqMQ 
- [x] Реализовано автоматическое подключение к правильным серверам почтовых сервисов, исходя из почтового домена вашего email-адреса  
- [x] Функциональные возможности работы с протоколами SMTP/IMAP:
  - [x] Возможность работать с разными папками почты 
  - [x] Возможность отправить письмо одному или нескольким адресатам с вложениями
  - [x] Возможность отправить запланированное на определенную дату и время письмо одному или нескольким адресатам с вложениями
  - [x] Возможность получить сообщения из почты, получение происходит через пагинацию и сортировано от самых новых к старым
  - [x] Возможность прочитать определенное сообщение из почты, а также скачать вложения
  - [x] Удаление сообщения из почты
  - [x] Перемещение сообщения из одной папки в другую
  - [x] Сохранение письма в базу данных PostgreSQL для долгосрочного хранения
- [x] Для получения писем реализованно кэширование через Redis. Использован механизм инвалидации кэша по принципу TTL(3 минуты) 
- [x] Реализован диспатчер и фоновый сервис, который контролирует активность клиентских сессий и закрывает IMAP и SMTP соединение, которое неактивность 15 минут
- [x] Реализован методы расширений для обеспечения более удобной функциональности библиотеки MailKit
- [ ] Реализована возможность сохранения вложений письма
- [ ] Реализована возможность сохранения физической копии письма в файловую систему
- [x] Реализованы механизм дружбы с обменом публичных RSA ключей
- [x] Реализованы крипто-провайдеры DES,MD5,RSA для отправки зашифрованных сообщений с проверкой ЭЦП
- [ ] Реализован механизм хранения cookies
- [ ] Реализована возможность создания и удаления отдельных папок, а также возможность создания иерархической структуры
- [ ] Реализован механизм взаимодействия с полным перечнем папок почтовых сервисов
- [ ] Реализована возможность взаимодействия с другими почтовыми сервисами




## Стек:
`в процессе написания`

## Установка и запуск

### Посредством Docker

`в процессе написания`

### Без использования Docker

`в процессе написания`

## Конфигурация

`в процессе написания`
