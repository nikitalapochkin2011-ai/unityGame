# Подключение настоящей рекламы AdMob

Кнопки и игровая логика уже связаны с вознаграждениями. В текущем архиве внешний SDK не установлен и аккаунт AdMob не настраивался. В Editor используется подписанный тестовый диалог, а без SDK на телефоне реклама недоступна.

## Подключение

1. Импортируйте официальный [Google Mobile Ads Unity plugin](https://github.com/googleads/googleads-mobile-unity/releases) по [инструкции Google](https://developers.google.com/admob/unity/quick-start). Выполните предусмотренную плагином настройку зависимостей Android.
2. В меню **Assets → Google Mobile Ads → Settings** укажите App ID приложения AdMob. App ID относится к приложению и отличается от ID рекламного блока.
3. В Unity **Project Settings → Player → Other Settings → Scripting Define Symbols** для Android добавьте **OMF_ADMOB**. Этот флаг включает готовый адаптер в Runtime/RewardedAdsService.cs. Без установленного плагина добавлять флаг нельзя: классы GoogleMobileAds.Api будут недоступны.
4. Откройте GameSettings.asset в Inspector, раздел **Ads**. Оставьте **Use Test Ads** включённым для тестирования на телефоне. Адаптер использует официальный тестовый rewarded-блок Google; оба места показа используют один сервис и по очереди загружаемые ролики.
5. Проверьте на устройстве оба сценария: просмотр до награды, закрытие без награды, недоступный интернет, повторные нажатия, сворачивание и возвращение. При успешном просмотре ежедневного вращения таймер должен сохраняться после перезапуска. После проигрыша должны сохраниться две потери и тот же третий пассажир с 30% терпения.
6. Для настоящих роликов укажите собственный **Android Rewarded Ad Unit Id** в GameSettings → Ads и выключите **Use Test Ads**. Для возможной iOS-сборки предусмотрено отдельное поле **Ios Rewarded Ad Unit Id**.

**Editor Simulation** действует только в редакторе: реальная награда на телефоне не выдаётся от тестового диалога. Рекламный SDK активируется только при OMF_ADMOB; отсутствие доступного ролика не списывает монеты и не потребляет ежедневный таймер. Игрок получает награду только после earned-callback SDK и завершения показа.

Адаптер использует RewardedAd.Load, CanShowAd, Show, OnAdFullScreenContentClosed, OnAdFullScreenContentFailed и Destroy. Callback переводятся в главный поток Unity через очередь. Текущий API сопоставлен с [официальным RewardedAd.cs](https://github.com/googleads/googleads-mobile-unity/blob/main/source/plugin/Assets/GoogleMobileAds/Api/RewardedAd.cs).

В этой среде нет Unity Editor/Android SDK: импорт внешнего плагина, компиляция его зависимостей, показ настоящего ролика и APK не проверялись. Установка плагина и выдача собственных идентификаторов — необходимые шаги перед использованием настоящей рекламы.
