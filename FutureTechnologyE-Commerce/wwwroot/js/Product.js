$(document).ready(function () {
  loadDataTable();
});

function loadDataTable() {
  $("#myTable").DataTable({
    processing: true,
    serverSide: true,
    ajax: {
      url: "/Product/GetAll",
      type: "GET",
      dataType: "json",
      data: function (d) {
        return {
          pageNumber: d.start / d.length + 1,
          pageSize: d.length,
          searchString: d.search.value,
        };
      },
      dataSrc: function (response) {
        // Set the total record count for pagination
        response.recordsTotal = response.totalCount;
        response.recordsFiltered = response.totalCount;
        return response.data;
      },
    },
    columns: [
      { data: "name" },
      { data: "description" },
      {
        data: "price",
        render: function (data) {
          return "$" + parseFloat(data).toFixed(2);
        },
      },
      { data: "categoryName" },
      { data: "brandName" },
      { data: "stockQuantity" },
      {
        data: "isBestseller",
        render: function (data, type, row) {
          return data
            ? '<span class="badge bg-success">Yes</span>'
            : '<span class="badge bg-secondary">No</span>';
        },
      },
      {
        data: "productID",
        render: function (data) {
          return `
                        <div class="btn-group" role="group">
                            <a href="/Product/Ubsert/${data}" class="btn btn-primary btn-sm">
                                <i class="bi bi-pencil-square"></i> Edit
                            </a>
                            <button onclick="Delete(${data})" class="btn btn-danger btn-sm">
                                <i class="bi bi-trash-fill"></i> Delete
                            </button>
                        </div>
                    `;
        },
      },
    ],
    responsive: true,
    autoWidth: false,
    language: {
      emptyTable: "No products found.",
    },
    columnDefs: [
      {
        targets: -1,
        orderable: false, // Disable sorting for the actions column
      },
    ],
    drawCallback: function (settings) {
      // Update UI after data is loaded
      var api = this.api();
      var pageInfo = api.page.info();
      $("#myTable_info").html(
        "Showing " +
          (pageInfo.start + 1) +
          " to " +
          pageInfo.end +
          " of " +
          pageInfo.recordsTotal +
          " entries"
      );
    },
  });
}

function Delete(id) {
  Swal.fire({
    title: "Confirm Delete?",
    text: "This action cannot be undone!",
    icon: "warning",
    showCancelButton: true,
    confirmButtonText: "Yes, delete it!",
  }).then((result) => {
    if (result.isConfirmed) {
      $.ajax({
        url: `/Product/Delete/${id}`,
        type: "DELETE",
        success: function (response) {
          toastr.success(response.message);
          $("#myTable").DataTable().ajax.reload();
        },
        error: function (xhr) {
          toastr.error(xhr.responseJSON.message);
        },
      });
    }
  });
}
