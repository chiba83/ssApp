using AutoMapper;
using ssAppModels.EFModels;
using ssAppBlazorWeb.Extensions;

public class MappingProfile : Profile
{
  public MappingProfile()
  {
    // 継承元プロパティもすべてマッピング
    CreateMap<DailyOrderNews, NewOrder>()
      .ForMember(dest => dest.GroupKey, opt => opt.Ignore());
  }
}
