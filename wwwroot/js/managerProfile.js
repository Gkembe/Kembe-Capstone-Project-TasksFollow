
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("managerProfileForm");

    if (form == null) {
        return;
    }

    form.addEventListener("submit", function (event) {

        var firstName = document.getElementById("firstName");
        var middleName = document.getElementById("middleName");
        var lastName = document.getElementById("lastName");
        var phone = document.getElementById("phone");

        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
            span.classList.remove("field-validation-error");
            span.classList.add("field-validation-valid");
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

        // Validate phone number
        var phonePattern = /^\+?[0-9\s\-()]{7,20}$/;

        if (phone.value.trim() !== "" && !phonePattern.test(phone.value.trim())) {
            showError("PhoneNumber", "Enter a valid phone number.");
            document.getElementById("phoneStart").textContent = "*";
            document.getElementById("phoneStart").style.color = "red";
            phone.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("phoneStart").textContent = "";
            phone.style.borderColor = "";
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
