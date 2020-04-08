function resetCounter() {
    var payload = {};
    payload.property_inspector = 'resetCounter';
    sendPayloadToPlugin(payload);
}

function openSaveFilePicker(title, filter, propertyName) {
    console.log("openSaveFilePicker called: ", title, filter, propertyName);
    var payload = {};
    payload.property_inspector = 'loadsavepicker';
    payload.picker_title = title;
    payload.picker_filter = filter;
    payload.property_name = propertyName;
    sendPayloadToPlugin(payload);
}
