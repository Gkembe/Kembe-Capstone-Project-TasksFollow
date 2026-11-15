
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("editEmployeeForm");

    form.addEventListener("submit", function (event) {

        var firstName = document.getElementById("firstName");
        var middleName = document.getElementById("middleName");
        var lastName = document.getElementById("lastName");
        var jobTitle = document.getElementById("jobTitle");
        var department = document.getElementById("department");

        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
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

        // Validate job title
        if (jobTitle.value.trim() === "") {
            showError("JobTitle", "Job title is required.");
            document.getElementById("jobTitleStart").textContent = "*";
            document.getElementById("jobTitleStart").style.color = "red";
            jobTitle.style.borderColor = "red";
            isValid = false;
        }
        else if (jobTitle.value.trim().length < 2) {
            showError("JobTitle", "Job title must be at least 2 characters.");
            document.getElementById("jobTitleStart").textContent = "*";
            document.getElementById("jobTitleStart").style.color = "red";
            jobTitle.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("jobTitleStart").textContent = "";
            jobTitle.style.borderColor = "";
        }

        // Validate department
        if (department.value.trim() === "") {
            showError("Department", "Department is required.");
            document.getElementById("departmentStart").textContent = "*";
            document.getElementById("departmentStart").style.color = "red";
            department.style.borderColor = "red";
            isValid = false;
        }
        else if (department.value.trim().length < 2) {
            showError("Department", "Department must be at least 2 characters.");
            document.getElementById("departmentStart").textContent = "*";
            document.getElementById("departmentStart").style.color = "red";
            department.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("departmentStart").textContent = "";
            department.style.borderColor = "";
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
