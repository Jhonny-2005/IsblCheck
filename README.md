# ISBL Check — Статический анализатор ISBL-кода

Инструмент для анализа ISBL кода, который служит для выявления ошибок в коде на стадии разработки. По сути является статическим анализатором кода.

**Версия 2.0.0** — Поддержка Directum 5.8

## Возможности

* Умеет выгружать разработку из базы данных, из пакета разработки (ISX-файла), а также из папки с разработкой, используемой утилитой [DTU](https://github.com/DirectumCompany/DevelopmentTransferUtility);
* Умеет отображать результаты проверки и подсвечивать ошибки прямо в вычислениях;
* Отчет о проверке отображается в окне утилиты и может быть сохранен в файл;
* Имеется консольный агент для проверки разработки в невизуальном режиме.

## Категории ошибок

* **ERROR** — Возможные Runtime ошибки;
* **WARNING** — Проверки на неоптимальный код;
* **INFO** — Прочие проверки.

## Правила проверки

### Базовые правила (F-коды)
* **F001** — Неверное количество параметров функции
* **F002** — Ошибка форматирования строки
* **F003** — Правило поиска单一值 для функции Format
* **F004** — Использование устаревших функций ISBL
* **F005** — Функция без справки
* **F006** — Функция слишком длинная
* **F007** — Использование несуществующей строки локализации
* **F008** — Не указан класс исключения
* **F009** — Использование несуществующего справочника
* **F010** — Использование интерактивных окон в событиях

### Правила переменных (A-коды)
* **A001** — Использование переменной без инициализации
* **A002** — Использование переменной с переопределением
* **A003** — Неиспользуемая переменная
* **A004** — Присваивание переменной самой себе

### Правила логических выражений (L-коды)
* **L001** — Использование ключевых слов True/False

### Правила объектной модели (O-коды)
* **O001** — Использование свойства Info.Reference
* **O002** — Объект не восстанавливает свое состояние

### Правила безопасности (S-коды)
* **S001** — Возможная SQL-инъекция через конкатенацию строк
* **S002** — Захардкоженные учётные данные

### Прочие правила
* **J001** — Комментарии TODO/DONE

## Directum 5.8: Что нового

* Обновлены системные функции ISBL для работы с Directum 5.8
* Добавлены интерфейсы для работы с почтой (IMailFactory, IMailServer, IMessage)
* Добавлены интерфейсы для серверных событий (IServerEvent, IServerEventFactory)
* Добавлены интерфейсы для глобальных ИД (IGlobalIDFactory, IGlobalIDInfo)
* Добавлены интерфейсы для процессов (IProcess, IProcessFactory, IProcessMessage)
* Добавлены новые типы управления (IPanelGroup, IInnerPanel, IBitButton)
* Добавлены новые значения TISBLContext для папок и процессов
* Добавлены новые предопределенные переменные (CurrentPeriod, Process, ProcessMessage, WorkTree)

## Состав сборок

[**IsblCheck**](https://github.com/DirectumCompany/IsblCheck/tree/master/src/IsblCheck) — GUI версия приложения

[**IsblCheck.Agent**](https://github.com/DirectumCompany/IsblCheck/tree/master/src/IsblCheck.Agent) — Консольная версия

[**IsblCheck.Core**](https://github.com/DirectumCompany/IsblCheck/tree/master/src/IsblCheck.Core) — Ядро

[**IsblCheck.Context.Development**](https://github.com/DirectumCompany/IsblCheck/tree/master/src/IsblCheck.Context.Development) — Контекст разработки

[**IsblCheck.Context.Application**](https://github.com/DirectumCompany/IsblCheck/tree/master/src/IsblCheck.Context.Application) — Контекст приложения

[**IsblCheck.Reports**](https://github.com/DirectumCompany/IsblCheck/tree/master/src/IsblCheck.Reports) — Работа с отчетами

## Сборка

### Установка необходимого ПО

* Visual Studio 2017 или новее
* [Расширение Antlr](https://visualstudiogallery.msdn.microsoft.com/25b991db-befd-441b-b23b-bb5f8d07ee9f)
* [Расширение SlowCheetah](https://github.com/Microsoft/slow-cheetah)
* [JRE](http://www.oracle.com/technetwork/java/javase/downloads/index.html)

Для сборки инсталляторов:
* WiX Toolset v3
* Расширение WiX для Visual Studio

### Порядок сборки

* Скачать проект через утилиты по работе с Git
* Восстановить зависимости решения через NuGet
* Выполнить сборку решения

## Лицензия

MIT License
