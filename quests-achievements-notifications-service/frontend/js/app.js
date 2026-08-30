(function () {
    "use strict";

    var dashboard = document.getElementById("dashboard");
    var signedOut = document.getElementById("signed-out");
    var serviceError = document.getElementById("service-error");
    var saveStatus = document.getElementById("save-status");
    var notifyAll = document.getElementById("notify-all");
    var notificationTypes = document.getElementById("notification-types");
    var notificationDialog = document.getElementById("notification-dialog");

    notifyAll.addEventListener("change", syncNotificationTypes);
    notificationDialog.addEventListener("click", function (event) {
        if (event.target === notificationDialog) {
            notificationDialog.close();
        }
    });
    notificationDialog.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            event.preventDefault();
            notificationDialog.close();
        }
    });

    document.body.addEventListener("htmx:configRequest", function (event) {
        var path = event.detail.path;
        if (path.endsWith("api/preferences") && !notifyAll.checked) {
            notificationTypes.querySelectorAll("input:checked").forEach(function (input) {
                event.detail.parameters[input.name] = input.value;
            });
        }
    });

    document.body.addEventListener("htmx:afterRequest", function (event) {
        var path = event.detail.pathInfo.requestPath;
        if (path.endsWith("api/profile")) {
            handleProfile(event.detail.xhr);
        } else if (path.endsWith("api/preferences")) {
            handlePreferences(event.detail.xhr);
        }
    });

    function handleProfile(xhr) {
        if (xhr.status === 401) {
            signedOut.hidden = false;
            return;
        }

        if (xhr.status !== 200) {
            serviceError.hidden = false;
            return;
        }

        renderProfile(JSON.parse(xhr.responseText));
        dashboard.hidden = false;
    }

    function renderProfile(profile) {
        var preferences = profile.preferences;
        var completed = profile.achievements.filter(function (achievement) {
            return achievement.progress >= achievement.progressNeeded;
        }).length;

        document.getElementById("profile-name").textContent = profile.username;
        document.getElementById("achievement-summary").textContent = completed + " of " + profile.achievements.length + " achievements complete";
        document.getElementById("completed-count").textContent = completed + " complete";
        document.getElementById("email").value = preferences.email || "";
        notifyAll.checked = preferences.notifyAll;
        setChecked("notifyPostEngagement", preferences.notifyPostEngagement);
        setChecked("notifyCourseCompletion", preferences.notifyCourseCompletion);
        setChecked("notifyQuizResults", preferences.notifyQuizResults);
        setChecked("notifyStreaks", preferences.notifyStreaks);
        setChecked("notifyAchievements", preferences.notifyAchievements);
        syncNotificationTypes();

        var grid = document.getElementById("achievement-grid");
        grid.replaceChildren();
        profile.achievements.forEach(function (achievement) {
            grid.appendChild(createAchievement(achievement));
        });

        renderNotifications(profile.notifications || []);
    }

    function createAchievement(achievement) {
        var fragment = document.getElementById("achievement-template").content.cloneNode(true);
        var article = fragment.querySelector("article");
        var image = fragment.querySelector("img");
        var complete = achievement.progress >= achievement.progressNeeded;

        article.dataset.complete = complete;
        image.src = achievement.image;
        image.addEventListener("error", function () {
            image.src = "images/achievement.svg";
        }, { once: true });
        fragment.querySelector(".lw-achievement__fallback").textContent = achievement.name.charAt(0);
        fragment.querySelector("h3").textContent = achievement.name;
        fragment.querySelector(".lw-achievement__state").textContent = complete ? "Earned" : "In progress";

        var progress = fragment.querySelector("progress");
        progress.value = achievement.progress;
        progress.max = achievement.progressNeeded;
        progress.setAttribute("aria-label", achievement.name + " progress");
        fragment.querySelector(".lw-achievement__progress").textContent = achievement.progress + " / " + achievement.progressNeeded;
        return fragment;
    }

    function renderNotifications(notifications) {
        var list = document.getElementById("notification-list");
        var empty = document.getElementById("notification-empty");
        list.replaceChildren();
        empty.hidden = notifications.length !== 0;
        document.getElementById("notification-count").textContent = notifications.length + (notifications.length === 1 ? " update" : " updates");

        notifications.forEach(function (notification) {
            var fragment = document.getElementById("notification-template").content.cloneNode(true);
            fragment.querySelector(".lw-notification__subject").textContent = notification.emailSubject;
            fragment.querySelector(".lw-notification__meta").textContent = formatTrigger(notification.trigger) + " · " + formatTime(notification.time);
            fragment.querySelector("button").addEventListener("click", function () {
                openNotification(notification);
            });
            list.appendChild(fragment);
        });
    }

    function openNotification(notification) {
        document.getElementById("notification-dialog-title").textContent = notification.emailSubject;
        document.getElementById("notification-dialog-meta").textContent = formatTrigger(notification.trigger) + " · " + formatTime(notification.time);
        document.getElementById("notification-dialog-body").textContent = notification.emailBody;
        notificationDialog.showModal();
    }

    function formatTrigger(trigger) {
        return trigger.split("-").map(function (word) {
            return word.charAt(0).toUpperCase() + word.slice(1);
        }).join(" ");
    }

    function formatTime(value) {
        var date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "Unknown time";
        }

        return new Intl.DateTimeFormat(undefined, {
            dateStyle: "medium",
            timeStyle: "short"
        }).format(date);
    }

    function handlePreferences(xhr) {
        saveStatus.dataset.error = xhr.status !== 200;
        if (xhr.status === 200) {
            saveStatus.textContent = JSON.parse(xhr.responseText).message;
        } else if (xhr.status === 400) {
            saveStatus.textContent = "Enter a valid email address.";
        } else {
            saveStatus.textContent = "Preferences could not be saved.";
        }
    }

    function setChecked(name, value) {
        document.querySelector('[name="' + name + '"]').checked = value;
    }

    function syncNotificationTypes() {
        notificationTypes.disabled = !notifyAll.checked;
    }
}());