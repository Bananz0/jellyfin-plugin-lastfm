const pluginId = "5e7fe7f0-b048-429e-a431-b1a7e69c930d";

const configDefaults = {
    Username: "",
    SessionKey: "",
    MediaBrowserUserId: "",
    LastfmApiHost: "ws.audioscrobbler.com",
    Options: {
        Scrobble: false,
        SyncFavourites: false,
        AlternativeMode: false
    }
};

let users = [];
let config = {};

function loadConfiguration() {
    return ApiClient.getPluginConfiguration(pluginId).then(function (c) {
        config = c;
    });
}

function loadUsers() {
    return ApiClient.getUsers().then(function (u) {
        users = u;
    });
}

function buildUserList(page) {
    page.querySelector("#user").innerHTML = users.map(function (u) {
        return '<option value="' + u.Id + '">' + u.Name + "</option>";
    }).join("");
}

function getCurrentSelectedUserId(page) {
    return page.querySelector("#user").value;
}

function getCurrentSelectedUser(page) {
    var id = getCurrentSelectedUserId(page);
    return (config.LastfmUsers || []).find(function (u) {
        return u.MediaBrowserUserId === id;
    });
}

function getUIOptionsValues(page) {
    return {
        Scrobble: page.querySelector("#optionScrobble").checked,
        SyncFavourites: page.querySelector("#optionFavourite").checked,
        AlternativeMode: page.querySelector("#optionAltMode").checked
    };
}

function populateInputs(page, userData) {
    var opts = Object.assign({}, configDefaults.Options,
        userData && userData.Options ? userData.Options : {});
    var data = Object.assign({}, configDefaults, userData || {}, { Options: opts });

    page.querySelector("#apiHost").value = config.LastfmApiHost || configDefaults.LastfmApiHost;
    page.querySelector("#username").value = data.Username;
    page.querySelector("#password").value = data.SessionKey;
    page.querySelector("#optionScrobble").checked = data.Options.Scrobble;
    page.querySelector("#optionFavourite").checked = data.Options.SyncFavourites;
    page.querySelector("#optionAltMode").checked = data.Options.AlternativeMode;
}

function onUserChange(page) {
    populateInputs(page, getCurrentSelectedUser(page));
}

function doSave(page) {
    return ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
        Dashboard.processPluginConfigurationUpdateResult(result);
        onUserChange(page);
    });
}

function save(page, username, password) {
    var userConfig = getCurrentSelectedUser(page);
    var previousApiHost = config.LastfmApiHost || configDefaults.LastfmApiHost;
    var newApiHost = page.querySelector("#apiHost").value || configDefaults.LastfmApiHost;
    var apiHostChanged = previousApiHost.trim().toLowerCase() !== newApiHost.trim().toLowerCase();

    config.LastfmApiHost = newApiHost;

    if (!username && !password) {
        doSave(page);
        return;
    }

    if (!userConfig) {
        userConfig = Object.assign({}, configDefaults,
            { MediaBrowserUserId: getCurrentSelectedUserId(page) });
        if (!config.LastfmUsers) config.LastfmUsers = [];
        config.LastfmUsers.push(userConfig);
    }

    userConfig.Options = getUIOptionsValues(page);

    Dashboard.showLoadingMsg();

    if (userConfig.SessionKey === password && !apiHostChanged) {
        doSave(page);
        return;
    }

    ApiClient.ajax({
        type: "POST",
        url: ApiClient.getUrl("Lastfm/Login"),
        data: JSON.stringify({ username: username, password: password, apiHost: config.LastfmApiHost }),
        contentType: "application/json",
        dataType: "json"
    }).then(function (data) {
        Dashboard.hideLoadingMsg();
        if (data && data.session) {
            userConfig.Username = data.session.name;
            userConfig.SessionKey = data.session.key;
            doSave(page);
        } else {
            Dashboard.alert((data && data.message) || "Something went wrong");
        }
    });
}

export default function (view) {
    view.querySelector("#LastfmScrobblerConfigurationForm").addEventListener("submit", function (e) {
        e.preventDefault();
        var username = view.querySelector("#username").value;
        var password = view.querySelector("#password").value;
        loadConfiguration().then(function () {
            save(view, username, password);
        });
    });

    view.querySelector("#user").addEventListener("change", function () {
        onUserChange(view);
    });

    view.addEventListener("viewshow", function () {
        var page = this;
        Dashboard.showLoadingMsg();
        Promise.all([
            loadConfiguration(),
            loadUsers().then(function () { buildUserList(page); })
        ]).then(function () {
            Dashboard.hideLoadingMsg();
            onUserChange(page);
        });
    });
}
