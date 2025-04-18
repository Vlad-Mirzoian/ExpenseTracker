# Трекер витрат з API Монобанку з можливістю створення груп

Цей проєкт реалізує трекер витрат, який інтегрується з API Монобанку для отримання витрат за останні місяці та дозволяє користувачам групувати витрати, надаючи додаткову статистику. Ідея полягає в тому, щоб створити можливість групувати витрати, наприклад, витрати на каву в різних точках мережі, де кожна точка може мати окрему назву термінала (наприклад, Lavazza в Запоріжжі).

## Опис

Проєкт дозволяє:

- **Отримання витрат**: Використовує API Монобанку для отримання витрат за останні місяці.
- **Групування витрат**: Дозволяє користувачам створювати групи для витрат (наприклад, групи для кави, магазину тощо) та групувати витрати по назвам терміналів.
- **Статистика**: Можливість отримувати статистику по групах витрат, включаючи суму витрат за певний період, кількість транзакцій тощо.
- **Гнучкість**: Користувач може самостійно визначити, які категорії витрат групувати разом.

## Технології

- **.NET 9** для серверної частини.
- **Razor Pages** для реалізації веб-інтерфейсу.
- **API Монобанку** для отримання даних про витрати.
- **JavaScript** для динамічного відображення даних і оновлення списку транзакцій.
- **Entity Framework Core** для роботи з базою даних (якщо потрібно зберігати додаткові дані про групи).

## Як працює

1. **Авторизація**: Користувачі авторизуються через JWT токени для доступу до свого акаунту в Монобанку.
2. **Отримання транзакцій**: Після авторизації застосунок витягує всі транзакції користувача за останні місяці через API Монобанку.
3. **Перегляд статистики**: Користувач може переглядати статистику для кожної групи, зокрема суму витрат, кількість транзакцій тощо.

## Як додавати нові групи та категорії

- Перейдіть у вкладку "Категорії" на сторінці шлюзу.
- Додайте нову категорію, вказавши назву та MCC коди для відповідних терміналів.
- Групуйте витрати за допомогою цих категорій.

