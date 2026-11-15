
document.addEventListener("DOMContentLoaded", function () {

    var form = document.querySelector("#createProjectForm, #editProjectForm");
    if (form == null) {
        return;
    }
    form.addEventListener("submit", function (event) {

        var projectName = document.getElementById("projectName");
        var description = document.getElementById("description");
        var startDate = document.getElementById("startDate");
        var dueDate = document.getElementById("dueDate");
        var status = document.getElementById("status");

        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
        });

        // Validate project name
        if (projectName.value.trim() === "") {
            showError("Name", "Project name is required.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            projectName.style.borderColor = "red";
            isValid = false;
        }
        else if (projectName.value.trim().length < 3) {
            showError("Name", "Project name must be at least 3 characters.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            projectName.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("nameStart").textContent = "";
            projectName.style.borderColor = "";
        }

        // Validate description
        if (description.value.trim().length > 2000) {
            showError("Description", "Project description cannot be more than 2000 characters.");
            document.getElementById("descStart").textContent = "*";
            document.getElementById("descStart").style.color = "red";
            description.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("descStart").textContent = "";
            description.style.borderColor = "";
        }

        // Validate start date
        if (startDate.value === "") {
            showError("StartDate", "Project start date is required.");
            document.getElementById("startDateStart").textContent = "*";
            document.getElementById("startDateStart").style.color = "red";
            startDate.style.borderColor = "red";
            isValid = false;
        }
        else if (isNaN(new Date(startDate.value).getTime())) {
            showError("StartDate", "Enter a valid start date.");
            document.getElementById("startDateStart").textContent = "*";
            document.getElementById("startDateStart").style.color = "red";
            startDate.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("startDateStart").textContent = "";
            startDate.style.borderColor = "";
        }

        // Validate due date
        if (dueDate.value === "") {
            showError("DueDate", "Project due date is required.");
            document.getElementById("dueDateStart").textContent = "*";
            document.getElementById("dueDateStart").style.color = "red";
            dueDate.style.borderColor = "red";
            isValid = false;
        }
        else if (isNaN(new Date(dueDate.value).getTime())) {
            showError("DueDate", "Enter a valid due date.");
            document.getElementById("dueDateStart").textContent = "*";
            document.getElementById("dueDateStart").style.color = "red";
            dueDate.style.borderColor = "red";
            isValid = false;
        }
        else if (startDate.value !== "" && !isNaN(new Date(startDate.value).getTime()) && dueDate.value < startDate.value) {
            showError("DueDate", "Due date cannot be earlier than start date.");
            document.getElementById("dueDateStart").textContent = "*";
            document.getElementById("dueDateStart").style.color = "red";
            dueDate.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("dueDateStart").textContent = "";
            dueDate.style.borderColor = "";
        }

        // Validate project status
        if (status.value === "") {
            showError("Status", "Project status is required.");
            document.getElementById("statusStart").textContent = "*";
            document.getElementById("statusStart").style.color = "red";
            status.style.borderColor = "red";
            isValid = false;
        }
        else if (status.value !== "Not Started" && status.value !== "In Progress" && status.value !== "Completed") {
            showError("Status", "Please select a valid project status.");
            document.getElementById("statusStart").textContent = "*";
            document.getElementById("statusStart").style.color = "red";
            status.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("statusStart").textContent = "";
            status.style.borderColor = "";
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
