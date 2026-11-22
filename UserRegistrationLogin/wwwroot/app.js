const form = document.querySelector("#auth-form");
const title = document.querySelector("#form-title");
const eyebrow = document.querySelector("#form-eyebrow");
const intro = document.querySelector("#form-intro");
const submitLabel = document.querySelector("#submit-label");
const submitButton = document.querySelector("#submit-button");
const password = document.querySelector("#password");
const passwordHint = document.querySelector("#password-hint");
const passwordToggle = document.querySelector("#toggle-password");
const alertBox = document.querySelector("#form-alert");
const modeButtons = [...document.querySelectorAll("[data-mode]")];
const fields = {
  email: document.querySelector("#email"),
  password,
};

fetch("/api/auth/me", { credentials: "same-origin" })
  .then((response) => {
    if (response.ok) {
      window.location.replace("/dashboard");
      return;
    }
    document.documentElement.classList.remove("checking-session");
  })
  .catch(() => document.documentElement.classList.remove("checking-session"));

let mode = "login";

function setMode(nextMode) {
  mode = nextMode;
  const registering = mode === "register";
  title.textContent = registering ? "Create your account" : "Welcome back";
  eyebrow.textContent = registering ? "A FRESH START" : "YOUR SPACE STARTS HERE";
  intro.textContent = registering
    ? "A few details and your space is ready."
    : "Sign in to pick up where you left off.";
  submitLabel.textContent = registering ? "Create account" : "Sign in";
  passwordHint.textContent = registering ? "8+ characters, a letter & number" : "Your password";
  password.autocomplete = registering ? "new-password" : "current-password";
  modeButtons.forEach((button) => {
    const active = button.dataset.mode === mode;
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-selected", String(active));
  });
  clearFeedback();
}

function clearFeedback() {
  alertBox.hidden = true;
  alertBox.textContent = "";
  alertBox.className = "alert";
  for (const name of Object.keys(fields)) {
    document.querySelector(`#${name}-error`).textContent = "";
    fields[name].removeAttribute("aria-invalid");
  }
}

for (const [name, field] of Object.entries(fields)) {
  field.addEventListener("input", () => {
    document.querySelector(`#${name}-error`).textContent = "";
    field.removeAttribute("aria-invalid");
    if (Object.values(fields).every((item) => !item.hasAttribute("aria-invalid"))) {
      alertBox.hidden = true;
      alertBox.textContent = "";
    }
  });
}

function showErrors(errors) {
  clearFeedback();
  if (errors && typeof errors === "object") {
    for (const [name, messages] of Object.entries(errors)) {
      if (!fields[name]) continue;
      fields[name].setAttribute("aria-invalid", "true");
      document.querySelector(`#${name}-error`).textContent = Array.isArray(messages)
        ? messages[0]
        : String(messages);
    }
  }
  alertBox.textContent = "Please review the highlighted fields.";
  alertBox.hidden = false;
}

function validateForm(payload) {
  const errors = {};
  if (!payload.email) errors.email = ["Email is required."];
  else if (payload.email.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(payload.email)) {
    errors.email = ["Enter a valid email address."];
  }

  if (!payload.password) errors.password = ["Password is required."];
  else if (mode === "register") {
    if (payload.password.length < 8) errors.password = ["Password must be at least 8 characters."];
    else if (payload.password.length > 128) errors.password = ["Password must be no more than 128 characters."];
    else if (!/[A-Za-z]/.test(payload.password) || !/[0-9]/.test(payload.password)) {
      errors.password = ["Password must contain at least one letter and one number."];
    }
  } else if (payload.password.length > 128) {
    errors.password = ["Password must be no more than 128 characters."];
  }
  return errors;
}

modeButtons.forEach((button) => button.addEventListener("click", () => setMode(button.dataset.mode)));

passwordToggle.addEventListener("click", () => {
  const reveal = password.type === "password";
  password.type = reveal ? "text" : "password";
  passwordToggle.textContent = reveal ? "Hide" : "Show";
  passwordToggle.setAttribute("aria-label", reveal ? "Hide password" : "Show password");
});

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearFeedback();
  const payload = {
    email: fields.email.value.trim(),
    password: fields.password.value,
  };
  const clientErrors = validateForm(payload);
  if (Object.keys(clientErrors).length > 0) {
    showErrors(clientErrors);
    return;
  }
  submitButton.disabled = true;
  submitButton.classList.add("is-loading");
  submitLabel.textContent = mode === "register" ? "Creating account…" : "Signing in…";

  try {
    const response = await fetch(`/api/auth/${mode === "register" ? "register" : "login"}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "same-origin",
      body: JSON.stringify(payload),
    });
    const result = response.status === 204 ? null : await response.json();

    if (!response.ok) {
      if (response.status === 400) showErrors(result?.fields);
      else {
        alertBox.textContent = result?.message || "Something went wrong. Please try again.";
        alertBox.hidden = false;
      }
      return;
    }

    if (mode === "register") {
      setMode("login");
      fields.email.value = payload.email;
      fields.password.value = "";
      alertBox.textContent = "Account created. Sign in with your new account.";
      alertBox.className = "alert is-success";
      alertBox.hidden = false;
    } else {
      window.location.assign("/dashboard");
    }
  } catch {
    alertBox.textContent = "We couldn't reach the server. Check your connection and try again.";
    alertBox.hidden = false;
  } finally {
    submitButton.disabled = false;
    submitButton.classList.remove("is-loading");
    submitLabel.textContent = mode === "register" ? "Create account" : "Sign in";
  }
});
