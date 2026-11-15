
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("createManagerForm");

    form.addEventListener("submit", function (event) {

        var firstName = document.getElementById("firstName");
        var middleName = document.getElementById("middleName");
        var lastName = document.getElementById("lastName");
        var email = document.getElementById("email");
        var password = document.getElementById("password");
        var confirmPassword = document.getElementById("confirmPassword");

        var isValid = true;

        // Clear previous error messages
        document.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
        });

        // Validate first name
        if (firstName.value.trim() === "") {
            showError("FirstName", "First name is required.");
            document.getElementById("firstNameStart").textContent = "*";
            document.getElementById("firstNameStart").style.color = "red";
            firstName.style.borderColor = "red";
            isValid = false;
        }
        else if (firstName.value.trim().length < 2) {
            showError("FirstName", "First name must be at least 2 characters.");
            document.getElementById("firstNameStart").textContent = "*";
            document.getElementById("firstNameStart").style.color = "red";
            firstName.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("firstNameStart").textContent = "";
            firstName.style.borderColor = "";
        }

        // Validate middle name
        if (middleName.value.trim().length > 100) {
            showError("MiddleName", "Middle name cannot be more than 100 characters.");
            document.getElementById("middleNameStart").textContent = "*";
            document.getElementById("middleNameStart").style.color = "red";
            middleName.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("middleNameStart").textContent = "";
            middleName.style.borderColor = "";
        }

        // Validate last name
        if (lastName.value.trim() === "") {
            showError("LastName", "Last name is required.");
            document.getElementById("lastNameStart").textContent = "*";
            document.getElementById("lastNameStart").style.color = "red";
            lastName.style.borderColor = "red";
            isValid = false;
        }
        else if (lastName.value.trim().length < 2) {
            showError("LastName", "Last name must be at least 2 characters.");
            document.getElementById("lastNameStart").textContent = "*";
            document.getElementById("lastNameStart").style.color = "red";
            lastName.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("lastNameStart").textContent = "";
            lastName.style.borderColor = "";
        }

        // Validate email
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (email.value.trim() === "") {
            showError("Email", "Email address is required.");
            document.getElementById("emailStart").textContent = "*";
            document.getElementById("emailStart").style.color = "red";
            email.style.borderColor = "red";
            isValid = false;
        }
        else if (!emailPattern.test(email.value.trim())) {
            showError("Email", "Enter a valid email address.");
            document.getElementById("emailStart").textContent = "*";
            document.getElementById("emailStart").style.color = "red";
            email.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("emailStart").textContent = "";
            email.style.borderColor = "";
        }

        // Validate password
        var passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$/;

        if (password.value === "") {
            showError("Password", "Password is required.");
            document.getElementById("passwordStart").textContent = "*";
            document.getElementById("passwordStart").style.color = "red";
            password.style.borderColor = "red";
            isValid = false;
        }
        else if (password.value.length < 6) {
            showError("Password", "Password must be at least 6 characters.");
            document.getElementById("passwordStart").textContent = "*";
            document.getElementById("passwordStart").style.color = "red";
            password.style.borderColor = "red";
            isValid = false;
        }
        else if (!passwordPattern.test(password.value)) {
            showError("Password", "Password must contain uppercase, lowercase, number and special character.");
            document.getElementById("passwordStart").textContent = "*";
            document.getElementById("passwordStart").style.color = "red";
            password.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("passwordStart").textContent = "";
            password.style.borderColor = "";
        }

        // Validate confirm password
        if (confirmPassword.value === "") {
            showError("ConfirmPassword", "Please confirm your password.");
            document.getElementById("confirmPasswordStart").textContent = "*";
            document.getElementById("confirmPasswordStart").style.color = "red";
            confirmPassword.style.borderColor = "red";
            isValid = false;
        }
        else if (confirmPassword.value !== password.value) {
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
        var errorSpan = document.querySelector('[data-valmsg-for="' + fieldName + '"]');

        if (errorSpan != null) {
            errorSpan.textContent = message;
            errorSpan.classList.remove("field-validation-valid");
            errorSpan.classList.add("field-validation-error");
        }
    }
});
