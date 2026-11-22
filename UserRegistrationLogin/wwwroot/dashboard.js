const emailElement = document.querySelector("#profile-email");
const avatar = document.querySelector("#avatar");
const logoutButton = document.querySelector("#logout-button");

async function loadProfile() {
  const response = await fetch("/api/auth/me", { credentials: "same-origin" });
  if (response.status === 401) {
    window.location.replace("/");
    return;
  }
  if (!response.ok) {
    emailElement.textContent = "Unable to load account";
    return;
  }

  const user = await response.json();
  emailElement.textContent = user.email;
  avatar.textContent = user.email.slice(0, 1).toUpperCase();
}

logoutButton.addEventListener("click", async () => {
  logoutButton.disabled = true;
  try {
    await fetch("/api/auth/logout", { method: "POST", credentials: "same-origin" });
  } finally {
    window.location.assign("/");
  }
});

loadProfile().catch(() => {
  emailElement.textContent = "Unable to load account";
});
