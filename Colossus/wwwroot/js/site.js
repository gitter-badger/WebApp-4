$(document).ready(function () {
    var uploadButton = $('#uploadButton');
    var copyButton = $('#copyButton');
    var openButton = $('#openButton');
    var settings = {
        onInit: function (elements) {
            uploadButton.hide();
            copyButton.hide();
            openButton.hide();
        },

        onGetFile: function (elements) {
            copyButton.hide();
            openButton.hide();
            uploadButton.show();
            uploadButton.val('Upload');
        },

        onStartSubmitting: function (elements) {
            if ('Notification' in window) {
                Notification.requestPermission((result) => { })
            }
            uploadButton.val('Uploading...');
            uploadButton.prop('disabled', true);
        },

        onProcessing: function (elements) {

        },

        onFinish: function (elements, data) {
            uploadButton.val('Upload');
            uploadButton.prop('disabled', false);
            copyButton.show();
            openButton.show();
            copyButton.attr('data-clipboard-text', data.value);
            openButton.attr('href', data.value);
            elements.message.html(data.value);
            var notification = new Notification('Upload Finished!', {
                icon: 'https://cdn.aiursoft.com/favicon.ico',
                body: "Your file was successfully uploaded!",
            });
            notification.onclick = function (event) {
                event.preventDefault();
                window.focus();
            };
        }
    };
    $('.dropify').setProgressedUploader(settings);
    $('.dropify').dropify();
});