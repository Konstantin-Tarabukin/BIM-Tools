using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RevitCopyParams
{
    public partial class ChangeJournalWindow : Window
    {
        public ChangeJournalWindow(
            UIApplication uiApp)
        {
            InitializeComponent();

            _uiApp = uiApp;

            RefreshJournal();
        }

        private readonly UIApplication _uiApp;

        private void RefreshJournal()
        {
            List<ChangeRecord> records =
                ChangeJournalService.GetJournalForCurrentModel(
                    _uiApp.ActiveUIDocument.Document);

            lvJournal.ItemsSource = null;
            lvJournal.ItemsSource = records;

            UpdateUnreadCounter(records);
            UpdateEmptyState(records);
        }

        private void UpdateUnreadCounter(
    List<ChangeRecord> records)
        {
            int unreadCount =
                records.Count(
                    record => record.Status == ChangeStatus.New);

            if (unreadCount > 0)
            {
                Title =
                    $"Журнал изменений — непрочитано: {unreadCount}";
            }
            else
            {
                Title =
                    "Журнал изменений";
            }
        }

        private void UpdateEmptyState(
    List<ChangeRecord> records)
        {
            bool hasRecords =
                records.Count > 0;

            lvJournal.Visibility =
                hasRecords
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            txtEmptyState.Visibility =
                hasRecords
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ChangeCreateWindow createWindow =
                new ChangeCreateWindow(
                    _uiApp.ActiveUIDocument.Document);

            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();

            if (result != true)
                return;

            ChangeRecord record =
                createWindow.ResultRecord;

            record.Author =
                _uiApp.Application.Username;

            record.SourceModel =
                RevitLinkService.GetModelName(
                    _uiApp.ActiveUIDocument.Document);

            ChangeExtensibleStorageService.AddRecord(
                _uiApp.ActiveUIDocument.Document,
                record);

            RefreshJournal();
        }

        private void OpenSelectedRecord()
        {
            ChangeRecord record =
                lvJournal.SelectedItem as ChangeRecord;

            if (record == null)
                return;

            ChangeDetailsWindow detailsWindow =
                new ChangeDetailsWindow(
                    _uiApp.ActiveUIDocument.Document,
                    record);

            detailsWindow.Owner = this;

            detailsWindow.ShowDialog();

            if (!detailsWindow.EditRequested)
            {
                RefreshJournal();
                return;
            }

            ChangeCreateWindow editWindow =
                new ChangeCreateWindow(
                    _uiApp.ActiveUIDocument.Document,
                    record);

            editWindow.Owner = this;

            bool? result =
                editWindow.ShowDialog();

            if (result != true)
            {
                RefreshJournal();
                return;
            }

            // --- АРХИТЕКТУРНАЯ ЗАЩИТА ---
            // Убеждаемся, что окно редактирования не пересоздало объект 
            // и не затерло ключевые поля, ломая логику уведомлений.
            ChangeRecord updatedRecord = editWindow.ResultRecord;

            updatedRecord.Id = record.Id;                 // Сохраняем старый Guid
            updatedRecord.CreatedDate = record.CreatedDate; // Сохраняем дату создания
            updatedRecord.Author = record.Author;           // Сохраняем автора
            updatedRecord.SourceModel = record.SourceModel; // Сохраняем модель-источник

            // Отмечаем, что запись была отредактирована
            updatedRecord.IsEdited = true;
            updatedRecord.ModifiedDate = DateTime.Now;

            ChangeExtensibleStorageService.UpdateRecord(
                _uiApp.ActiveUIDocument.Document,
                updatedRecord);

            RefreshJournal();
        }

        private void lvJournal_MouseDoubleClick(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenSelectedRecord();
        }

        private void lvJournal_KeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;

            OpenSelectedRecord();

            e.Handled = true;
        }
    }
}