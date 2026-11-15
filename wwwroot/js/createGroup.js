
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("createGroupForm");

    form.addEventListener("submit", function (event) {

        var groupName = document.getElementById("groupName");
        var description = document.getElementById("description");
        var employeeContainer = document.getElementById("employeeContainer");
        var selectedEmployees = document.querySelectorAll('input[name="SelectedEmployeeIds"]:checked');

        var isValid = true;

        // Clear previous error messages
        document.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
        });

        // Validate group name
        if (groupName.value.trim() === "") {
            showError("Name", "Group name is required.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            groupName.style.borderColor = "red";
            isValid = false;
        }
        else if (groupName.value.trim().length < 3) {
            showError("Name", "Group name must be at least 3 characters.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            groupName.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("nameStart").textContent = "";
            groupName.style.borderColor = "";
        }

        // Validate description
        if (description.value.trim().length > 2000) {
            showError("Description", "Group description cannot be more than 2000 characters.");
            document.getElementById("descStart").textContent = "*";
            document.getElementById("descStart").style.color = "red";
            description.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("descStart").textContent = "";
            description.style.borderColor = "";
        }

        // Validate selected employees
        if (selectedEmployees.length === 0) {
            showError("SelectedEmployeeIds", "Please select at least one employee.");
            document.getElementById("employeeStart").textContent = "*";
            document.getElementById("employeeStart").style.color = "red";
            employeeContainer.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("employeeStart").textContent = "";
            employeeContainer.style.borderColor = "";
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
