Imports System.IO
Imports Newtonsoft.Json

Public Module LocalizationService

    Public Enum SupportedLanguage
        English
        Italian
        Spanish
        French
        German
        Japanese
        Portuguese
        Russian
    End Enum

    Private _currentLanguage As SupportedLanguage = SupportedLanguage.English
    Private _translations As Dictionary(Of String, String)
    Private ReadOnly LanguageFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.json")

    ''' <summary>
    ''' Initialize localization system
    ''' </summary>
    Public Sub Initialize(Optional language As SupportedLanguage = SupportedLanguage.English)
        _currentLanguage = language
        LoadTranslations()
    End Sub

    ''' <summary>
    ''' Get translation for key
    ''' </summary>
    Public Function T(key As String) As String
        If _translations IsNot Nothing AndAlso _translations.ContainsKey(key) Then
            Return _translations(key).Replace("\n", Environment.NewLine)
        End If
        Return key ' Return key if translation not found
    End Function

    ''' <summary>
    ''' Set current language
    ''' </summary>
    Public Sub SetLanguage(language As SupportedLanguage)
        _currentLanguage = language
        LoadTranslations()
        SaveLanguagePreference()
    End Sub

    ''' <summary>
    ''' Get current language
    ''' </summary>
    Public Function GetCurrentLanguage() As SupportedLanguage
        Return _currentLanguage
    End Function

    ''' <summary>
    ''' Load language preference
    ''' </summary>
    Public Function LoadLanguagePreference() As SupportedLanguage
        Try
            If File.Exists(LanguageFilePath) Then
                Dim json = File.ReadAllText(LanguageFilePath)
                Dim pref = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(json)
                If pref.ContainsKey("language") Then
                    Return [Enum].Parse(GetType(SupportedLanguage), pref("language"))
                End If
            End If
        Catch ex As Exception
            ' Return default
        End Try
        Return SupportedLanguage.English
    End Function

    ''' <summary>
    ''' Save language preference
    ''' </summary>
    Private Sub SaveLanguagePreference()
        Try
            Dim pref As New Dictionary(Of String, String) From {
                {"language", _currentLanguage.ToString()}
            }
            Dim json = JsonConvert.SerializeObject(pref, Formatting.Indented)
            File.WriteAllText(LanguageFilePath, json)
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ''' <summary>
    ''' Load translations for current language
    ''' </summary>
    Private Sub LoadTranslations()
        _translations = New Dictionary(Of String, String)

        Select Case _currentLanguage
            Case SupportedLanguage.English
                LoadEnglishTranslations()
            Case SupportedLanguage.Italian
                LoadItalianTranslations()
            Case SupportedLanguage.Spanish
                LoadSpanishTranslations()
            Case SupportedLanguage.French
                LoadFrenchTranslations()
            Case SupportedLanguage.German
                LoadGermanTranslations()
            Case SupportedLanguage.Japanese
                LoadJapaneseTranslations()
            Case SupportedLanguage.Portuguese
                LoadPortugueseTranslations()
            Case SupportedLanguage.Russian
                LoadRussianTranslations()
        End Select
    End Sub

    Private Sub LoadEnglishTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "Select Game Folder"},
            {"start_patch", "Start Patching"},
            {"stop", "Stop"},
            {"backup_create", "Create Backup"},
            {"verify_integrity", "Verify Integrity"},
            {"export_report", "Export Report"},
            {"recent_folders", "Recent Folders"},
            {"batch_mode", "Batch Mode"},
            {"preset", "Preset"},
            {"target_sdk", "Target SDK"},
            {"status", "Status"},
            {"ready", "Ready"},
            {"processing", "Processing..."},
            {"completed", "Completed"},
            {"failed", "Failed"},
            {"files_patched", "Files Patched"},
            {"files_skipped", "Files Skipped"},
            {"files_failed", "Files Failed"},
            {"success_rate", "Success Rate"},
            {"statistics", "Statistics"},
            {"settings", "Settings"},
            {"language", "Language"},
            {"theme", "Theme"},
            {"auto_backup", "Auto Backup"},
            {"auto_verify", "Auto Verify"},
            {"about", "About"},
            {"credits", "Credits"},
            {"help", "Help"},
            {"exit", "Exit"},
            {"btn_browse", "Browse"},
            {"btn_start", "Start Cooking"},
            {"btn_recent", "📂 Recent"},
            {"lbl_preset", "Preset:"},
            {"lbl_language", "🌍 Language:"},
            {"hint_dragdrop", "💡 TIP: Drag & Drop game folders directly onto the path field below!"},
            {"btn_statistics", "📊 Statistics"},
            {"btn_elf_inspector", "🔍 ELF Inspector"},
            {"btn_batch", "📦 Batch Process"},
            {"statistics_title", "Statistics Dashboard"},
            {"statistics_error", "Error loading statistics"},
            {"elf_inspector_title", "ELF Inspector - SDK Analysis"},
            {"elf_inspector_select_folder", "Please select a game folder first!"},
            {"elf_inspector_error", "Error analyzing folder"},
            {"batch_title", "Batch Processing"},
            {"batch_description", "Batch Processing lets you patch multiple game folders at once!\n\nWould you like to select folders to process?\n\nNote: All games will use the currently selected SDK version."},
            {"batch_select_folder", "Select game folder {0} (Cancel when done)"},
            {"batch_added", "Added"},
            {"batch_starting", "Starting Batch Process: {0} folders"},
            {"batch_processing", "[{0}/{1}] Processing"},
            {"batch_skipped", "Skipped: Invalid game folder"},
            {"batch_completed", "Completed: Patched {0}, Skipped {1}"},
            {"batch_error", "Error"},
            {"batch_complete", "Batch Complete: {0} Success, {1} Failed"},
            {"batch_complete_msg", "Batch Processing Complete!\n\nSuccess: {0}\nFailed: {1}"},
            {"batch_error_msg", "Error in batch processing"},
            {"error_title", "Error"},
            {"placeholder_path", "Select PS5 Game Folder (any name supported)"},
            {"recommended", "(Recommended)"},
            {"not_recommended", "Not Recommended"},
            {"checking_selfutils", "Checking for SelfUtils"},
            {"checking_fakelibs", "Checking fakelibs!"},
            {"checking_updates", "Checking for Updates"},
            {"checks_done", "Checks Done!"}
        }
    End Sub

    Private Sub LoadItalianTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "Seleziona Cartella Gioco"},
            {"start_patch", "Avvia Patching"},
            {"stop", "Ferma"},
            {"backup_create", "Crea Backup"},
            {"verify_integrity", "Verifica Integrità"},
            {"export_report", "Esporta Report"},
            {"recent_folders", "Cartelle Recenti"},
            {"batch_mode", "Modalità Batch"},
            {"preset", "Preset"},
            {"target_sdk", "SDK Destinazione"},
            {"status", "Stato"},
            {"ready", "Pronto"},
            {"processing", "Elaborazione..."},
            {"completed", "Completato"},
            {"failed", "Fallito"},
            {"files_patched", "File Patchati"},
            {"files_skipped", "File Saltati"},
            {"files_failed", "File Falliti"},
            {"success_rate", "Tasso di Successo"},
            {"statistics", "Statistiche"},
            {"settings", "Impostazioni"},
            {"language", "Lingua"},
            {"theme", "Tema"},
            {"auto_backup", "Backup Automatico"},
            {"auto_verify", "Verifica Automatica"},
            {"about", "Informazioni"},
            {"credits", "Crediti"},
            {"help", "Aiuto"},
            {"exit", "Esci"},
            {"btn_browse", "Sfoglia"},
            {"btn_start", "Inizia Cottura"},
            {"btn_recent", "📂 Recenti"},
            {"lbl_preset", "Preset:"},
            {"lbl_language", "🌍 Lingua:"},
            {"hint_dragdrop", "💡 CONSIGLIO: Trascina le cartelle dei giochi direttamente nel campo percorso qui sotto!"},
            {"btn_statistics", "📊 Statistiche"},
            {"btn_elf_inspector", "🔍 Ispettore ELF"},
            {"btn_batch", "📦 Elaborazione Batch"},
            {"statistics_title", "Dashboard Statistiche"},
            {"statistics_error", "Errore nel caricamento delle statistiche"},
            {"elf_inspector_title", "Ispettore ELF - Analisi SDK"},
            {"elf_inspector_select_folder", "Seleziona prima una cartella gioco!"},
            {"elf_inspector_error", "Errore nell'analisi della cartella"},
            {"batch_title", "Elaborazione Batch"},
            {"batch_description", "L'Elaborazione Batch ti permette di patchare più cartelle di giochi contemporaneamente!\n\nVuoi selezionare le cartelle da elaborare?\n\nNota: Tutti i giochi useranno la versione SDK attualmente selezionata."},
            {"batch_select_folder", "Seleziona cartella gioco {0} (Annulla quando finito)"},
            {"batch_added", "Aggiunto"},
            {"batch_starting", "Avvio Elaborazione Batch: {0} cartelle"},
            {"batch_processing", "[{0}/{1}] Elaborazione"},
            {"batch_skipped", "Saltato: Cartella gioco non valida"},
            {"batch_completed", "Completato: Patchati {0}, Saltati {1}"},
            {"batch_error", "Errore"},
            {"batch_complete", "Batch Completato: {0} Successo, {1} Falliti"},
            {"batch_complete_msg", "Elaborazione Batch Completata!\n\nSuccesso: {0}\nFalliti: {1}"},
            {"batch_error_msg", "Errore nell'elaborazione batch"},
            {"error_title", "Errore"},
            {"placeholder_path", "Seleziona Cartella Gioco PS5 (qualsiasi nome supportato)"},
            {"recommended", "(Consigliato)"},
            {"not_recommended", "Non Consigliato"},
            {"checking_selfutils", "Controllo SelfUtils"},
            {"checking_fakelibs", "Controllo fakelibs!"},
            {"checking_updates", "Controllo Aggiornamenti"},
            {"checks_done", "Controlli Completati!"}
        }
    End Sub

    Private Sub LoadSpanishTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "Seleccionar Carpeta del Juego"},
            {"start_patch", "Iniciar Parche"},
            {"stop", "Detener"},
            {"backup_create", "Crear Respaldo"},
            {"verify_integrity", "Verificar Integridad"},
            {"export_report", "Exportar Informe"},
            {"recent_folders", "Carpetas Recientes"},
            {"batch_mode", "Modo por Lotes"},
            {"preset", "Preajuste"},
            {"target_sdk", "SDK Objetivo"},
            {"status", "Estado"},
            {"ready", "Listo"},
            {"processing", "Procesando..."},
            {"completed", "Completado"},
            {"failed", "Fallido"},
            {"files_patched", "Archivos Parcheados"},
            {"files_skipped", "Archivos Omitidos"},
            {"files_failed", "Archivos Fallidos"},
            {"success_rate", "Tasa de Éxito"},
            {"statistics", "Estadísticas"},
            {"settings", "Configuración"},
            {"language", "Idioma"},
            {"theme", "Tema"},
            {"auto_backup", "Respaldo Automático"},
            {"auto_verify", "Verificación Automática"},
            {"about", "Acerca de"},
            {"credits", "Créditos"},
            {"help", "Ayuda"},
            {"exit", "Salir"}
        }
    End Sub

    Private Sub LoadFrenchTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "Sélectionner le Dossier du Jeu"},
            {"start_patch", "Démarrer le Patch"},
            {"stop", "Arrêter"},
            {"backup_create", "Créer une Sauvegarde"},
            {"verify_integrity", "Vérifier l'Intégrité"},
            {"export_report", "Exporter le Rapport"},
            {"recent_folders", "Dossiers Récents"},
            {"batch_mode", "Mode Batch"},
            {"preset", "Préréglage"},
            {"target_sdk", "SDK Cible"},
            {"status", "Statut"},
            {"ready", "Prêt"},
            {"processing", "Traitement..."},
            {"completed", "Terminé"},
            {"failed", "Échoué"},
            {"files_patched", "Fichiers Patchés"},
            {"files_skipped", "Fichiers Ignorés"},
            {"files_failed", "Fichiers Échoués"},
            {"success_rate", "Taux de Réussite"},
            {"statistics", "Statistiques"},
            {"settings", "Paramètres"},
            {"language", "Langue"},
            {"theme", "Thème"},
            {"auto_backup", "Sauvegarde Automatique"},
            {"auto_verify", "Vérification Automatique"},
            {"about", "À Propos"},
            {"credits", "Crédits"},
            {"help", "Aide"},
            {"exit", "Quitter"}
        }
    End Sub

    Private Sub LoadGermanTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "Spielordner Auswählen"},
            {"start_patch", "Patch Starten"},
            {"stop", "Stoppen"},
            {"backup_create", "Backup Erstellen"},
            {"verify_integrity", "Integrität Überprüfen"},
            {"export_report", "Bericht Exportieren"},
            {"recent_folders", "Letzte Ordner"},
            {"batch_mode", "Stapelverarbeitung"},
            {"preset", "Voreinstellung"},
            {"target_sdk", "Ziel-SDK"},
            {"status", "Status"},
            {"ready", "Bereit"},
            {"processing", "Verarbeitung..."},
            {"completed", "Abgeschlossen"},
            {"failed", "Fehlgeschlagen"},
            {"files_patched", "Dateien Gepatcht"},
            {"files_skipped", "Dateien Übersprungen"},
            {"files_failed", "Dateien Fehlgeschlagen"},
            {"success_rate", "Erfolgsrate"},
            {"statistics", "Statistiken"},
            {"settings", "Einstellungen"},
            {"language", "Sprache"},
            {"theme", "Thema"},
            {"auto_backup", "Auto-Backup"},
            {"auto_verify", "Auto-Überprüfung"},
            {"about", "Über"},
            {"credits", "Danksagungen"},
            {"help", "Hilfe"},
            {"exit", "Beenden"},
            {"btn_browse", "Durchsuchen"},
            {"btn_start", "Kochen Starten"},
            {"btn_recent", "📂 Letzte"},
            {"lbl_preset", "Voreinstellung:"},
            {"lbl_language", "🌍 Sprache:"},
            {"hint_dragdrop", "💡 TIPP: Ziehen Sie Spielordner direkt in das Pfadfeld unten!"},
            {"btn_statistics", "📊 Statistiken"},
            {"btn_elf_inspector", "🔍 ELF-Inspektor"},
            {"btn_batch", "📦 Stapelverarbeitung"},
            {"statistics_title", "Statistik-Dashboard"},
            {"statistics_error", "Fehler beim Laden der Statistiken"},
            {"elf_inspector_title", "ELF-Inspektor - SDK-Analyse"},
            {"elf_inspector_select_folder", "Bitte wählen Sie zuerst einen Spielordner!"},
            {"elf_inspector_error", "Fehler bei der Analyse des Ordners"},
            {"batch_title", "Stapelverarbeitung"},
            {"batch_description", "Die Stapelverarbeitung ermöglicht es Ihnen, mehrere Spielordner gleichzeitig zu patchen!\n\nMöchten Sie Ordner zur Verarbeitung auswählen?\n\nHinweis: Alle Spiele verwenden die aktuell ausgewählte SDK-Version."},
            {"batch_select_folder", "Spielordner {0} auswählen (Abbrechen wenn fertig)"},
            {"batch_added", "Hinzugefügt"},
            {"batch_starting", "Starte Stapelverarbeitung: {0} Ordner"},
            {"batch_processing", "[{0}/{1}] Verarbeitung"},
            {"batch_skipped", "Übersprungen: Ungültiger Spielordner"},
            {"batch_completed", "Abgeschlossen: Gepatcht {0}, Übersprungen {1}"},
            {"batch_error", "Fehler"},
            {"batch_complete", "Stapel Abgeschlossen: {0} Erfolg, {1} Fehlgeschlagen"},
            {"batch_complete_msg", "Stapelverarbeitung Abgeschlossen!\n\nErfolg: {0}\nFehlgeschlagen: {1}"},
            {"batch_error_msg", "Fehler bei der Stapelverarbeitung"},
            {"error_title", "Fehler"},
            {"placeholder_path", "PS5-Spielordner auswählen (beliebiger Name unterstützt)"},
            {"recommended", "(Empfohlen)"},
            {"not_recommended", "Nicht Empfohlen"},
            {"checking_selfutils", "Prüfe SelfUtils"},
            {"checking_fakelibs", "Prüfe fakelibs!"},
            {"checking_updates", "Prüfe Updates"},
            {"checks_done", "Prüfungen Abgeschlossen!"}
        }
    End Sub

    Private Sub LoadJapaneseTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "ゲームフォルダを選択"},
            {"start_patch", "パッチ開始"},
            {"stop", "停止"},
            {"backup_create", "バックアップ作成"},
            {"verify_integrity", "整合性検証"},
            {"export_report", "レポート出力"},
            {"recent_folders", "最近のフォルダ"},
            {"batch_mode", "バッチモード"},
            {"preset", "プリセット"},
            {"target_sdk", "ターゲットSDK"},
            {"status", "状態"},
            {"ready", "準備完了"},
            {"processing", "処理中..."},
            {"completed", "完了"},
            {"failed", "失敗"},
            {"files_patched", "パッチ済みファイル"},
            {"files_skipped", "スキップ済みファイル"},
            {"files_failed", "失敗ファイル"},
            {"success_rate", "成功率"},
            {"statistics", "統計"},
            {"settings", "設定"},
            {"language", "言語"},
            {"theme", "テーマ"},
            {"auto_backup", "自動バックアップ"},
            {"auto_verify", "自動検証"},
            {"about", "について"},
            {"credits", "クレジット"},
            {"help", "ヘルプ"},
            {"exit", "終了"}
        }
    End Sub

    Private Sub LoadPortugueseTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "Selecionar Pasta do Jogo"},
            {"start_patch", "Iniciar Patch"},
            {"stop", "Parar"},
            {"backup_create", "Criar Backup"},
            {"verify_integrity", "Verificar Integridade"},
            {"export_report", "Exportar Relatório"},
            {"recent_folders", "Pastas Recentes"},
            {"batch_mode", "Modo em Lote"},
            {"preset", "Predefinição"},
            {"target_sdk", "SDK Alvo"},
            {"status", "Status"},
            {"ready", "Pronto"},
            {"processing", "Processando..."},
            {"completed", "Concluído"},
            {"failed", "Falhado"},
            {"files_patched", "Arquivos Corrigidos"},
            {"files_skipped", "Arquivos Ignorados"},
            {"files_failed", "Arquivos Falhados"},
            {"success_rate", "Taxa de Sucesso"},
            {"statistics", "Estatísticas"},
            {"settings", "Configurações"},
            {"language", "Idioma"},
            {"theme", "Tema"},
            {"auto_backup", "Backup Automático"},
            {"auto_verify", "Verificação Automática"},
            {"about", "Sobre"},
            {"credits", "Créditos"},
            {"help", "Ajuda"},
            {"exit", "Sair"}
        }
    End Sub

    Private Sub LoadRussianTranslations()
        _translations = New Dictionary(Of String, String) From {
            {"app_title", "PS5 BackPork Kitchen"},
            {"select_folder", "Выбрать папку игры"},
            {"start_patch", "Начать патч"},
            {"stop", "Остановить"},
            {"backup_create", "Создать резервную копию"},
            {"verify_integrity", "Проверить целостность"},
            {"export_report", "Экспортировать отчет"},
            {"recent_folders", "Последние папки"},
            {"batch_mode", "Пакетный режим"},
            {"preset", "Пресет"},
            {"target_sdk", "Целевой SDK"},
            {"status", "Статус"},
            {"ready", "Готово"},
            {"processing", "Обработка..."},
            {"completed", "Завершено"},
            {"failed", "Не удалось"},
            {"files_patched", "Файлы пропатчены"},
            {"files_skipped", "Файлы пропущены"},
            {"files_failed", "Файлы не удалось"},
            {"success_rate", "Уровень успеха"},
            {"statistics", "Статистика"},
            {"settings", "Настройки"},
            {"language", "Язык"},
            {"theme", "Тема"},
            {"auto_backup", "Авто-резервирование"},
            {"auto_verify", "Авто-проверка"},
            {"about", "О программе"},
            {"credits", "Благодарности"},
            {"help", "Помощь"},
            {"exit", "Выход"}
        }
    End Sub

    ''' <summary>
    ''' Get all available languages
    ''' </summary>
    Public Function GetAvailableLanguages() As List(Of String)
        Return [Enum].GetNames(GetType(SupportedLanguage)).ToList()
    End Function

End Module