
document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("createPaymentForm");

    if (form == null) {
        return;
    }

    form.addEventListener("submit", function (event) {

        var subscriptionPlanId = document.getElementById("subscriptionPlanId");
        var amountPaid = document.getElementById("amountPaid");

        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
            span.classList.remove("field-validation-error");
            span.classList.add("field-validation-valid");
        });

        // Validate subscription plan
        if (subscriptionPlanId.value === "") {
            showError("SubscriptionPlanId", "Please select a subscription plan.");
            document.getElementById("planStart").textContent = "*";
            document.getElementById("planStart").style.color = "red";
            subscriptionPlanId.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("planStart").textContent = "";
            subscriptionPlanId.style.borderColor = "";
        }

        // Validate amount paid
        if (amountPaid.value.trim() === "") {
            showError("AmountPaid", "Payment amount is required.");
            document.getElementById("amountStart").textContent = "*";
            document.getElementById("amountStart").style.color = "red";
            amountPaid.style.borderColor = "red";
            isValid = false;
        }
        else if (!Number.isFinite(Number(amountPaid.value)) || Number(amountPaid.value) <= 0) {
            showError("AmountPaid", "Payment amount must be greater than zero.");
            document.getElementById("amountStart").textContent = "*";
            document.getElementById("amountStart").style.color = "red";
            amountPaid.style.borderColor = "red";
            isValid = false;
        }
        else if (!/^\d+(\.\d{1,2})?$/.test(amountPaid.value)) {
            showError("AmountPaid", "Payment amount cannot have more than 2 decimal places.");
            document.getElementById("amountStart").textContent = "*";
            document.getElementById("amountStart").style.color = "red";
            amountPaid.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("amountStart").textContent = "";
            amountPaid.style.borderColor = "";
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
