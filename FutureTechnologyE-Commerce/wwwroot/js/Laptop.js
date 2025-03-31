
$(document).ready(function () {
   
});

function Delete(id) {
    Swal.fire({
        title: 'Confirm Delete?',
        text: "This action cannot be undone!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/Laptop/Delete/${id}`,
                type: 'DELETE',
                success: function (response) {
                    toastr.success(response.message);
                    $('#myTable').DataTable().ajax.reload();
                },
                error: function (xhr) {
                    toastr.error(xhr.responseJSON.message);
                }
            });
        }
    });