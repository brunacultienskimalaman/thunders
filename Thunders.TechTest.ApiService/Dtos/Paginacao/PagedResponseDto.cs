namespace Thunders.TechTest.ApiService.Dtos.Paginacao
{
    public class PagedResponseDto<T>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        public static PagedResponseDto<T> Create(List<T> data, int page, int pageSize, int totalRecords)
        {
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return new PagedResponseDto<T>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }
    }
}
