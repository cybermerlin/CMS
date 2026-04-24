Feature: GitHub authentication via Device Flow
  Чтобы начать отслеживание Discussions
  Как пользователь приложения
  Я хочу авторизоваться через GitHub Device Flow

  Background:
    Given приложение запущено

  Scenario: Successful authorization via Device Code
    When пользователь нажимает кнопку "Авторизоваться"
    Then приложение запрашивает "Device Code" и "User Code" у GitHub
      And приложение открывает браузер на странице "https://github.com/login/oauth/access_token"
      And приложение копирует "User Code" в буфер обмена и отображает его на экране
      And приложение начинает фоновый опрос (polling) сервера GitHub
    When пользователь вводит код в браузере и подтверждает доступ
    Then приложение получает "Access Token"
      And токен сохраняется в безопасном хранилище
      And пользователь видит сообщение об успешном подключении

  Scenario: User didn't set Client ID of this App
    When пользователь инициирует авторизацию
    Then приложение проверяет, что у него есть Client ID
      When если Client ID отсутствует
      Then показывает окно с полем для вводе Client ID и Инструкцией как его сделать
        When пользователь создал Client ID по Инструкции
          And ввел его в поле
          And нажал кнопку Сохранить
        Then Client ID сохранился в безопасном месте
          And начался выполняться сценарий Авторизации

  Scenario: User cancels or expires authorization
    When пользователь инициирует авторизацию
    Then приложение ожидает ввод кода пользователем
    When время действия кода истекает или пользователь отменяет запрос на сайте GitHub
    Then приложение прекращает опрос сервера
      And пользователь видит сообщение об ошибке или истечении времени
