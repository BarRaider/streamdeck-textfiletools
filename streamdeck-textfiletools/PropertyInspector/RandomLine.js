document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    showHideSettings(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            showHideSettings(payload.settings);
        }
    });
});

function showHideSettings(payload) {
    console.log("Show Hide Settings Called");
    setOutputFileName("none");
    if (payload['outputAction'] == 2) {
        setOutputFileName("");
    }
}

function setOutputFileName(displayValue) {
    var dvOutputFileName = document.getElementById('dvOutputFileName');
    dvOutputFileName.style.display = displayValue;
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
