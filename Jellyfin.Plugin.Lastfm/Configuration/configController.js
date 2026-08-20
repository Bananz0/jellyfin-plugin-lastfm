const pluginId = "f5cc9733-e4df-42f3-a950-12d62d5819cc";

const configDefaults = {
    Username: "",
    SessionKey: "",
    MediaBrowserUserId: "",
    LastfmApiHost: "ws.audioscrobbler.com",
    Options: {
        Scrobble: false,
        SyncFavourites: false,
        SyncPlayCount: false,
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
        SyncPlayCount: page.querySelector("#optionSyncPlayCount").checked,
        AlternativeMode: page.querySelector("#optionAltMode").checked
    };
}

function populateInputs(page, userData) {
    var opts = Object.assign({}, configDefaults.Options,
        userData && userData.Options ? userData.Options : {});
    var data = Object.assign({}, configDefaults, userData || {}, { Options: opts });

    page.querySelector("#apiKey").value    = config.ApiKey    || "";
    page.querySelector("#apiSecret").value = config.ApiSecret || "";
    page.querySelector("#apiHost").value   = config.LastfmApiHost || configDefaults.LastfmApiHost;
    page.querySelector("#username").value  = data.Username;
    page.querySelector("#password").value  = data.SessionKey;
    page.querySelector("#optionScrobble").checked       = data.Options.Scrobble;
    page.querySelector("#optionFavourite").checked      = data.Options.SyncFavourites;
    page.querySelector("#optionSyncPlayCount").checked  = data.Options.SyncPlayCount;
    page.querySelector("#optionAltMode").checked        = data.Options.AlternativeMode;
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
    var newApiHost      = page.querySelector("#apiHost").value    || configDefaults.LastfmApiHost;
    var newApiKey       = page.querySelector("#apiKey").value     || "";
    var newApiSecret    = page.querySelector("#apiSecret").value  || "";
    var apiHostChanged  = previousApiHost.trim().toLowerCase() !== newApiHost.trim().toLowerCase();

    config.LastfmApiHost = newApiHost;

    if (!username && !password) {
        config.ApiKey    = newApiKey;
        config.ApiSecret = newApiSecret;
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
        config.ApiKey    = newApiKey;
        config.ApiSecret = newApiSecret;
        doSave(page);
        return;
    }

    ApiClient.ajax({
        type: "POST",
        url: ApiClient.getUrl("Lastfm/Login"),
        data: JSON.stringify({
            username:  username,
            password:  password,
            apiHost:   newApiHost,
            apiKey:    newApiKey,
            apiSecret: newApiSecret
        }),
        contentType: "application/json",
        dataType: "json"
    }).then(function (data) {
        Dashboard.hideLoadingMsg();
        if (data && data.session) {
            userConfig.Username   = data.session.name;
            userConfig.SessionKey = data.session.key;
            config.ApiKey         = newApiKey;
            config.ApiSecret      = newApiSecret;
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
