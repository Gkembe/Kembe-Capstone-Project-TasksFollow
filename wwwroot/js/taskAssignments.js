
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("assignEmployeeForm");

    // Validate employee assignment
    if (form != null) {

        form.addEventListener("submit", function (event) {

            var employeeId = document.getElementById("employeeId");
            var isValid = true;

            // Clear previous error messages
            form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
                span.textContent = "";
                span.classList.remove("field-validation-error");
                span.classList.add("field-validation-valid");
            });

            // Validate selected employee
            if (employeeId.value === "") {
                showError("employeeId", "Please select a project member.");
                document.getElementById("employeeStart").textContent = "*";
                document.getElementById("employeeStart").style.color = "red";
                employeeId.style.borderColor = "red";
                isValid = false;
            }
            else {
                document.getElementById("employeeStart").textContent = "";
                employeeId.style.borderColor = "";
            }

            // Prevent form submission if validation fails
            if (!isValid) {
                event.preventDefault();
            }
        });
    }

    // Display error message below the field
    function showError(fieldName, message) {
        var errorSpan = form.querySelector('[data-valmsg-for="' + fieldName + '"]');

        if (errorSpan != null) {
            errorSpan.textContent = message;
            errorSpan.classList.remove("field-validation-valid");
            errorSpan.classList.add("field-validation-error");
        }
    }

    // Confirm before removing a task assignment
    var removeForms = document.querySelectorAll(".removeAssignmentForm");

    removeForms.forEach(function (removeForm) {

        removeForm.addEventListener("submit", function (event) {

            var confirmation = confirm("Remove this task assignment?");

            if (!confirmation) {
                event.preventDefault();
            }
        });
    });
});
