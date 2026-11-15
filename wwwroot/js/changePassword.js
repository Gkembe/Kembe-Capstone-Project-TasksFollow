
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("changePasswordForm");

    if (form == null) {
        return;
    }

    form.addEventListener("submit", function (event) {

        var currentPassword = document.getElementById("currentPassword");
        var newPassword = document.getElementById("newPassword");
        var confirmPassword = document.getElementById("confirmPassword");

        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
            span.classList.remove("field-validation-error");
            span.classList.add("field-validation-valid");
        });

        // Validate current password
        if (currentPassword.value.trim() === "") {
            showError("CurrentPassword", "Current password is required.");
            document.getElementById("currentPasswordStart").textContent = "*";
            document.getElementById("currentPasswordStart").style.color = "red";
            currentPassword.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("currentPasswordStart").textContent = "";
            currentPassword.style.borderColor = "";
        }

        // Validate new password
        var passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$/;

        if (newPassword.value === "") {
            showError("NewPassword", "New password is required.");
            document.getElementById("newPasswordStart").textContent = "*";
            document.getElementById("newPasswordStart").style.color = "red";
            newPassword.style.borderColor = "red";
            isValid = false;
        }
        else if (newPassword.value.length < 6) {
            showError("NewPassword", "New password must be at least 6 characters.");
            document.getElementById("newPasswordStart").textContent = "*";
            document.getElementById("newPasswordStart").style.color = "red";
            newPassword.style.borderColor = "red";
            isValid = false;
        }
        else if (!passwordPattern.test(newPassword.value)) {
            showError("NewPassword", "Password must contain uppercase, lowercase, number and special character.");
            document.getElementById("newPasswordStart").textContent = "*";
            document.getElementById("newPasswordStart").style.color = "red";
            newPassword.style.borderColor = "red";
            isValid = false;
        }
        else if (newPassword.value === currentPassword.value) {
            showError("NewPassword", "New password must be different from current password.");
            document.getElementById("newPasswordStart").textContent = "*";
            document.getElementById("newPasswordStart").style.color = "red";
            newPassword.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("newPasswordStart").textContent = "";
            newPassword.style.borderColor = "";
        }

        // Validate confirm password
        if (confirmPassword.value === "") {
            showError("ConfirmPassword", "Please confirm your new password.");
            document.getElementById("confirmPasswordStart").textContent = "*";
            document.getElementById("confirmPasswordStart").style.color = "red";
            confirmPassword.style.borderColor = "red";
            isValid = false;
        }
        else if (confirmPassword.value !== newPassword.value) {
            showError("ConfirmPassword", "Passwords do not match.");
            document.getElementById("confirmPasswordStart").textContent = "*";
            document.getElementById("confirmPasswordStart").style.color = "red";
            confirmPassword.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("confirmPasswordStart").textContent = "";
            confirmPassword.style.borderColor = "";
        }

        // Prevent form submission if validation fails
        if (!isValid) {
            event.preventDefault();
        }
    });

    // Display error message below the field
    function showError(fieldName, message) {
        var errorSpan = form.querySelector('[data-valmsg-for="' + fieldName + '"]');

        if (errorSpan != null) {
            errorSpan.textContent = message;
            errorSpan.classList.remove("field-validation-valid");
            errorSpan.classList.add("field-validation-error");
        }
    }
});
